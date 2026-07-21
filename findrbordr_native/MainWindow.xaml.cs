using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace findrbordr_native
{
    public partial class MainWindow : Window
    {
        #region Win32 API Imports
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(
            IntPtr hWnd,
            StringBuilder lpClassName,
            int nMaxCount
        );

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            out RECT pvAttribute,
            int cbAttribute
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // --- Event Hooking APIs ---
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime
        );

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags
        );

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(
            uint uAction,
            uint uParam,
            StringBuilder lpvParam,
            uint fuWinIni
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left,
                Top,
                Right,
                Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const uint SPI_GETDESKWALLPAPER = 0x0073;
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        private const int GWL_HWNDPARENT = -8;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int OBJID_WINDOW = 0;
        #endregion

        private IntPtr targetExplorerHwnd = IntPtr.Zero;
        private dynamic? wshellInstance;

        private WinEventDelegate? winEventDelegate;
        private IntPtr locationHook = IntPtr.Zero;
        private IntPtr foregroundHook = IntPtr.Zero;
        private IntPtr destroyHook = IntPtr.Zero;
        private IntPtr showHook = IntPtr.Zero;
        private IntPtr nameHook = IntPtr.Zero;
        private ImageBrush? wallpaperBrush;
        private double screenWidth = SystemParameters.PrimaryScreenWidth;
        private double screenHeight = SystemParameters.PrimaryScreenHeight;

        public class ShortcutItem
        {
            public string Title { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
        }

        public static class ShortcutStorage
        {
            private static readonly string FilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "custom_shortcuts.json"
            );

            public static ObservableCollection<ShortcutItem> LoadShortcuts()
            {
                if (!File.Exists(FilePath))
                    return new ObservableCollection<ShortcutItem>();

                try
                {
                    string json = File.ReadAllText(FilePath);
                    var items = JsonSerializer.Deserialize<ObservableCollection<ShortcutItem>>(
                        json
                    );
                    return items ?? new ObservableCollection<ShortcutItem>();
                }
                catch
                {
                    return new ObservableCollection<ShortcutItem>();
                }
            }

            public static void SaveShortcuts(ObservableCollection<ShortcutItem> shortcuts)
            {
                try
                {
                    string json = JsonSerializer.Serialize(
                        shortcuts,
                        new JsonSerializerOptions { WriteIndented = true }
                    );
                    File.WriteAllText(FilePath, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Gagal menyimpan JSON: " + ex.Message);
                }
            }
        }

        private ObservableCollection<ShortcutItem> customShortcuts =
            new ObservableCollection<ShortcutItem>();

        private void InitCustomShortcuts()
        {
            try
            {
                customShortcuts = ShortcutStorage.LoadShortcuts();

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                        {
                            listControl.ItemsSource = customShortcuts;
                        }
                    }),
                    DispatcherPriority.Loaded
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal memuat kustom shortcut: " + ex.Message);
            }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (files != null && files.Length > 0)
                {
                    bool hasNewItems = false;

                    foreach (string path in files)
                    {
                        if (
                            !customShortcuts.Any(s =>
                                s.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                        {
                            string name = System.IO.Path.GetFileName(path);

                            if (string.IsNullOrEmpty(name))
                                name = path;

                            customShortcuts.Add(new ShortcutItem { Title = name, Path = path });

                            hasNewItems = true;
                        }
                    }

                    if (hasNewItems)
                    {
                        ShortcutStorage.SaveShortcuts(customShortcuts);

                        Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                if (
                                    this.FindName("CustomShortcutsList") is ItemsControl listControl
                                )
                                {
                                    listControl.ItemsSource = null;
                                    listControl.ItemsSource = customShortcuts;
                                }
                            }),
                            DispatcherPriority.Render
                        );
                    }
                }
            }
        }

        private void BtnDeleteShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is ShortcutItem itemToRemove)
            {
                customShortcuts.Remove(itemToRemove);
                ShortcutStorage.SaveShortcuts(customShortcuts);

                if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                {
                    listControl.ItemsSource = null;
                    listControl.ItemsSource = customShortcuts;
                }
            }
        }

        // --- APP RELAUNCH & EXIT HANDLERS ---
        private void Sidebar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Suppress the sidebar context menu if right-clicking directly on a child element that handles its own context menu
            if (
                e.OriginalSource is FrameworkElement element
                && element.ContextMenu != null
                && element.ContextMenu != (sender as FrameworkElement)?.ContextMenu
            )
            {
                e.Handled = true;
            }
        }

        private void BtnRelaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(exePath);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to relaunch application: " + ex.Message);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public MainWindow()
        {
            InitCustomShortcuts();
            InitializeComponent();

            Type? wshellType = Type.GetTypeFromProgID("WScript.Shell");
            if (wshellType != null)
                wshellInstance = Activator.CreateInstance(wshellType);

            this.Loaded += MainWindow_Loaded;
            this.LocationChanged += (s, e) => UpdateParallaxOffset();

            // Memastikan setiap kali area WPF ditekan, Explorer langsung fokus
            this.PreviewMouseDown += (s, e) =>
            {
                if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                {
                    SetForegroundWindow(targetExplorerHwnd);
                }
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDesktopWallpaper();
            UpdateParallaxOffset();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            long exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(
                hwnd,
                GWL_EXSTYLE,
                (IntPtr)(exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW)
            );

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    RegisterWinEventHooks();
                }),
                DispatcherPriority.Render
            );
        }

        private void RegisterWinEventHooks()
        {
            winEventDelegate = new WinEventDelegate(WinEventCallback);

            locationHook = SetWinEventHook(
                EVENT_OBJECT_LOCATIONCHANGE,
                EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );

            foregroundHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );

            destroyHook = SetWinEventHook(
                EVENT_OBJECT_DESTROY,
                EVENT_OBJECT_HIDE,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );

            showHook = SetWinEventHook(
                EVENT_OBJECT_SHOW,
                EVENT_OBJECT_SHOW,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );

            nameHook = SetWinEventHook(
                EVENT_OBJECT_NAMECHANGE,
                EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );

            TryFindAndAttachExplorer();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (locationHook != IntPtr.Zero)
                UnhookWinEvent(locationHook);
            if (foregroundHook != IntPtr.Zero)
                UnhookWinEvent(foregroundHook);
            if (destroyHook != IntPtr.Zero)
                UnhookWinEvent(destroyHook);
            if (showHook != IntPtr.Zero)
                UnhookWinEvent(showHook);
            if (nameHook != IntPtr.Zero)
                UnhookWinEvent(nameHook);

            base.OnClosed(e);
        }

        private void WinEventCallback(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime
        )
        {
            if (idObject != OBJID_WINDOW)
                return;

            if (
                (eventType == EVENT_OBJECT_DESTROY || eventType == EVENT_OBJECT_HIDE)
                && hwnd == targetExplorerHwnd
            )
            {
                DetachAndHide();
                Dispatcher.BeginInvoke(
                    new Action(() => TryFindAndAttachExplorer()),
                    DispatcherPriority.Background
                );
                return;
            }

            if (eventType == EVENT_OBJECT_SHOW || eventType == EVENT_SYSTEM_FOREGROUND)
            {
                if (targetExplorerHwnd == IntPtr.Zero || IsCabinetWindow(hwnd))
                {
                    CheckAndUpdateTargetExplorer(hwnd);
                }
            }
            else if (
                (eventType == EVENT_OBJECT_LOCATIONCHANGE || eventType == EVENT_OBJECT_NAMECHANGE)
                && hwnd == targetExplorerHwnd
            )
            {
                SyncOverlayPosition();
            }
        }

        private void CheckAndUpdateTargetExplorer(IntPtr foregroundHwnd)
        {
            if (IsCabinetWindow(foregroundHwnd) && IsWindowVisible(foregroundHwnd))
            {
                if (targetExplorerHwnd != foregroundHwnd)
                {
                    AttachToExplorer(foregroundHwnd);
                }
                else
                {
                    SyncOverlayPosition();
                }
            }
            else
            {
                if (targetExplorerHwnd != IntPtr.Zero)
                {
                    if (!IsWindow(targetExplorerHwnd) || !IsWindowVisible(targetExplorerHwnd))
                    {
                        DetachAndHide();
                    }
                }
            }
        }

        private void DetachAndHide()
        {
            targetExplorerHwnd = IntPtr.Zero;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, IntPtr.Zero);

            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
        }

        private void TryFindAndAttachExplorer()
        {
            IntPtr fgHwnd = GetForegroundWindow();
            if (IsCabinetWindow(fgHwnd))
            {
                AttachToExplorer(fgHwnd);
                return;
            }

            IntPtr foundHwnd = IntPtr.Zero;
            EnumWindows(
                (hwnd, lParam) =>
                {
                    if (IsWindowVisible(hwnd) && IsCabinetWindow(hwnd))
                    {
                        foundHwnd = hwnd;
                        return false;
                    }
                    return true;
                },
                IntPtr.Zero
            );

            if (foundHwnd != IntPtr.Zero)
            {
                AttachToExplorer(foundHwnd);
            }
            else
            {
                Hide();
            }
        }

        private bool IsCabinetWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            StringBuilder cName = new StringBuilder(256);
            GetClassName(hwnd, cName, cName.Capacity);
            return cName.ToString() == "CabinetWClass";
        }

        private void AttachToExplorer(IntPtr explorerHwnd)
        {
            targetExplorerHwnd = explorerHwnd;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, targetExplorerHwnd);
            SyncOverlayPosition();
        }

        private void SyncOverlayPosition()
        {
            if (targetExplorerHwnd == IntPtr.Zero || !IsWindowVisible(targetExplorerHwnd))
            {
                if (Visibility == Visibility.Visible)
                    Hide();
                return;
            }

            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            if (
                DwmGetWindowAttribute(
                    targetExplorerHwnd,
                    DWMWA_EXTENDED_FRAME_BOUNDS,
                    out RECT rect,
                    Marshal.SizeOf(typeof(RECT))
                ) == 0
            )
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (Visibility != Visibility.Visible)
                    Show();

                SetWindowPos(
                    overlayHwnd,
                    IntPtr.Zero,
                    rect.Left - 7,
                    rect.Top - 7,
                    width + 14,
                    height + 14,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW
                );

                UpdateParallaxOffset();

                IntPtr currentTarget = targetExplorerHwnd;
                System.Threading.Tasks.Task.Run(() =>
                {
                    StringBuilder sbTitle = new StringBuilder(256);
                    GetWindowText(currentTarget, sbTitle, sbTitle.Capacity);

                    string rawTitle = sbTitle.ToString().Trim();
                    string folderName = "Explorer";

                    if (!string.IsNullOrWhiteSpace(rawTitle))
                    {
                        string cleanTitle = Regex
                            .Replace(rawTitle, @" and \d+ more tabs", "", RegexOptions.IgnoreCase)
                            .Replace(" - File Explorer", "")
                            .Replace(" - Windows Explorer", "")
                            .Replace(" and 1 more tab", "")
                            .Trim();

                        try
                        {
                            string cleanPath = cleanTitle.TrimEnd(
                                Path.DirectorySeparatorChar,
                                Path.AltDirectorySeparatorChar
                            );

                            string extractedName = Path.GetFileName(cleanPath);
                            folderName = !string.IsNullOrWhiteSpace(extractedName)
                                ? extractedName
                                : cleanTitle;
                        }
                        catch
                        {
                            folderName = cleanTitle;
                        }
                    }

                    Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            if (
                                currentTarget == targetExplorerHwnd
                                && TxtTitle != null
                                && TxtTitle.Text != folderName
                            )
                            {
                                TxtTitle.Text = folderName;
                            }
                        })
                    );
                });
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                // Jika target File Explorer valid, paksa Windows untuk memfokuskannya
                if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                {
                    SetForegroundWindow(targetExplorerHwnd);
                }

                handled = true;
                return (IntPtr)MA_NOACTIVATE;
            }
            return IntPtr.Zero;
        }

        private void SendKeysToExplorer(string keys)
        {
            if (targetExplorerHwnd == IntPtr.Zero || !IsWindow(targetExplorerHwnd))
                return;

            try
            {
                // 1. Paksa Windows fokus ke File Explorer terlebih dahulu
                SetForegroundWindow(targetExplorerHwnd);

                // 2. Jalankan SendKeys async dengan sedikit jeda agar Windows sempat memproses fokus
                System
                    .Threading.Tasks.Task.Delay(30)
                    .ContinueWith(_ =>
                    {
                        try
                        {
                            wshellInstance?.SendKeys(keys);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("SendKeys error: " + ex.Message);
                        }
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Focus/SendKeys error: " + ex.Message);
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (
                sender is Button btn
                && btn.Tag is string pathTarget
                && targetExplorerHwnd != IntPtr.Zero
            )
            {
                string targetPath = pathTarget;
                if (Enum.TryParse(pathTarget, out Environment.SpecialFolder folder))
                {
                    targetPath = Environment.GetFolderPath(folder);
                }

                SendKeysToExplorer("^l");
                System
                    .Threading.Tasks.Task.Delay(50)
                    .ContinueWith(_ =>
                    {
                        string escapedPath = targetPath
                            .Replace("~", "{~}")
                            .Replace("(", "{(}")
                            .Replace(")", "{)}");
                        SendKeysToExplorer(escapedPath + "{ENTER}");
                    });
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("%{LEFT}");

        private void BtnForward_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("%{RIGHT}");

        private void BtnUp_Click(object sender, RoutedEventArgs e) => SendKeysToExplorer("%{UP}");

        private void BtnViewIcons_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("^+2");

        private void BtnViewList_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("^+5");

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("^+6");

        private void BtnViewTiles_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("^+7");

        private void BtnTogglePreview_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("%p");

        private void BtnToggleDetailsPane_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("+%p");

        private void BtnProperties_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("%{ENTER}");

        private void BtnContextMenu_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("+{F10}");

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => SendKeysToExplorer("^f");

        private void BtnFolderOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("control.exe", "folders");
            }
            catch { }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) =>
            SendMessage(targetExplorerHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) =>
            SendMessage(targetExplorerHwnd, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero);

        private void BtnMaximize_Click(object sender, RoutedEventArgs e) =>
            SendMessage(
                targetExplorerHwnd,
                WM_SYSCOMMAND,
                IsZoomed(targetExplorerHwnd) ? (IntPtr)SC_RESTORE : (IntPtr)SC_MAXIMIZE,
                IntPtr.Zero
            );

        private void LoadDesktopWallpaper()
        {
            try
            {
                StringBuilder wallPaperPath = new StringBuilder(260);
                SystemParametersInfo(
                    SPI_GETDESKWALLPAPER,
                    (uint)wallPaperPath.Capacity,
                    wallPaperPath,
                    0
                );

                string path = wallPaperPath.ToString();

                if (File.Exists(path) && ParallaxCanvas != null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    wallpaperBrush = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.Fill,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        ViewportUnits = BrushMappingMode.Absolute,

                        RelativeTransform = new ScaleTransform(1.1, 1.1, 0.5, 0.5),
                    };

                    ParallaxCanvas.Background = wallpaperBrush;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal memuat wallpaper: " + ex.Message);
            }
        }

        private void UpdateParallaxOffset()
        {
            if (wallpaperBrush == null || ParallaxCanvas == null || !ParallaxCanvas.IsLoaded)
                return;

            try
            {
                Point screenOrigin = ParallaxCanvas.PointFromScreen(new Point(0, 0));

                wallpaperBrush.Viewport = new Rect(
                    screenOrigin.X,
                    screenOrigin.Y,
                    screenWidth,
                    screenHeight
                );
            }
            catch { }
        }
    }
}
