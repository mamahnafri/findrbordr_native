using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace findrbordr_native
{
    #region ==================== DATA MODELS & JSON SETTINGS ====================

    public class ShadowSettings
    {
        public double BlurRadius { get; set; } = 30;
        public double Opacity { get; set; } = 0.12;
        public double ShadowDepth { get; set; } = 5;
        public string ColorHex { get; set; } = "#000000";
    }

    public class BrushSettings
    {
        public string ColorHex { get; set; } = "#FFFFFF";
        public double Opacity { get; set; } = 0.9;
    }

    /// <summary>
    /// Model utama konfigurasi aplikasi yang memuat Dynamic Resource XAML & Shadow Settings
    /// </summary>
    public class AppSettings
    {
        // --- Dynamic Resource Brushes ---
        public double SidebarWidth { get; set; } = 185;
        public string CapsuleBackgroundBrush { get; set; } = "#FFFFFF";
        public string MainTextBrush { get; set; } = "#2C3E50";
        public string OuterFrameBrush { get; set; } = "#FFFFFF";

        // --- Drop Shadows & Borders ---
        public BrushSettings Layer3BorderBackground { get; set; } = new BrushSettings();
        public ShadowSettings LeftSidebarGridShadow { get; set; } = new ShadowSettings();
        public ShadowSettings CapsuleStyleShadow { get; set; } = new ShadowSettings();

        // --- Shortcuts ---
        public ObservableCollection<MainWindow.ShortcutItem> CustomShortcuts { get; set; } =
            new ObservableCollection<MainWindow.ShortcutItem>();
    }

    public static class SettingsStorage
    {
        private static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "app_settings.json"
        );

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(FilePath))
            {
                // Buat file JSON default jika belum ada saat compile/first run
                var defaultSettings = new AppSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal memuat JSON settings: " + ex.Message);
                return new AppSettings();
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal menyimpan JSON settings: " + ex.Message);
            }
        }
    }

    #endregion

    public partial class MainWindow : Window
    {
        #region ==================== WIN32 API IMPORTS & STRUCTS ====================

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

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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

        #endregion

        #region ==================== FIELDS & PRIVATE VARIABLES ====================

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
        private const uint SWP_NOCOPYBITS = 0x0100;
        private const uint SWP_DEFERERASE = 0x2000;
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int OBJID_WINDOW = 0;

        private DispatcherTimer? syncThrottleTimer;
        private bool isSyncPending = false;
        private bool isFocusStateRefreshQueued = false;
        private bool isTitleRefreshQueued = false;
        private bool lastExplorerFocusVisualState = false;

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

        public string UserProfileName { get; set; }

        private AppSettings appSettings = new AppSettings();
        private ObservableCollection<ShortcutItem> customShortcuts =
            new ObservableCollection<ShortcutItem>();

        private string lastKnownFolderName = "Explorer";
        private readonly Stopwatch pathCheckStopwatch = Stopwatch.StartNew();
        private long lastPathCheckMs = -1000;
        private const int PATH_CHECK_THROTTLE_MS = 300;
        private const int MAX_FOLDER_TITLE_LENGTH = 32;
        private readonly StringBuilder classNameBuffer = new StringBuilder(256);

        private static readonly Regex TabSuffixRegex = new Regex(
            @" and \d+ more tabs",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public class ShortcutItem
        {
            public string Title { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
        }

        #endregion

        #region ==================== CONSTRUCTOR & INITIALIZATION ====================

        public MainWindow()
        {
            InitSettingsAndShortcuts();
            InitializeComponent();

            syncThrottleTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Render,
                (s, e) =>
                {
                    if (isSyncPending)
                    {
                        isSyncPending = false;
                        SyncOverlayPositionInternal();
                    }
                },
                Dispatcher.CurrentDispatcher
            );
            syncThrottleTimer.Stop();

            Type? wshellType = Type.GetTypeFromProgID("WScript.Shell");
            if (wshellType != null)
                wshellInstance = Activator.CreateInstance(wshellType);

            this.Loaded += MainWindow_Loaded;
            this.LocationChanged += (s, e) => UpdateParallaxOffset();

            UserProfileName = Environment.UserName;
            this.DataContext = this;

            this.PreviewMouseDown += (s, e) =>
            {
                if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                {
                    SetForegroundWindow(targetExplorerHwnd);
                }
            };
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDesktopWallpaperAsync();
            UpdateParallaxOffset();
            ApplyAppSettingsToUI();
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
                new Action(() => RegisterWinEventHooks()),
                DispatcherPriority.Render
            );
        }

        #endregion

        #region ==================== APP SETTINGS & SHORTCUTS LOGIC ====================

        private void InitSettingsAndShortcuts()
        {
            try
            {
                appSettings = SettingsStorage.LoadSettings();
                customShortcuts = appSettings.CustomShortcuts;

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                        {
                            listControl.ItemsSource = customShortcuts;
                        }

                        ApplyAppSettingsToUI();
                    }),
                    DispatcherPriority.Loaded
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal memuat setting & shortcut: " + ex.Message);
            }
        }

        /// <summary>
        /// Menerapkan setting Dynamic Resource Brush & DropShadow dari JSON langsung ke UI
        /// </summary>
        private void ApplyAppSettingsToUI()
        {
            try
            {
                // Cari ColumnDefinition berdasarkan x:Name "SidebarColumn"
                if (
                    this.FindName("SidebarColumn") is ColumnDefinition sidebarCol
                    && appSettings.SidebarWidth > 0
                )
                {
                    sidebarCol.Width = new GridLength(appSettings.SidebarWidth);
                }

                // 1. Update Dynamic Resource Brushes XAML langsung dari JSON
                if (!string.IsNullOrEmpty(appSettings.CapsuleBackgroundBrush))
                {
                    var capsuleColor = (Color)
                        ColorConverter.ConvertFromString(appSettings.CapsuleBackgroundBrush);
                    this.Resources["CapsuleBackgroundBrush"] = new SolidColorBrush(capsuleColor);
                }

                if (!string.IsNullOrEmpty(appSettings.MainTextBrush))
                {
                    var textColor = (Color)
                        ColorConverter.ConvertFromString(appSettings.MainTextBrush);
                    this.Resources["MainTextBrush"] = new SolidColorBrush(textColor);
                }

                if (!string.IsNullOrEmpty(appSettings.OuterFrameBrush))
                {
                    var outerColor = (Color)
                        ColorConverter.ConvertFromString(appSettings.OuterFrameBrush);
                    this.Resources["OuterFrameBrush"] = new SolidColorBrush(outerColor);
                }

                // 2. Terapkan DropShadow pada LeftSidebarGrid Shadow Border
                if (
                    this.FindName("LeftSidebarShadowBorder") is Border shadowBorder
                    && appSettings.LeftSidebarGridShadow != null
                )
                {
                    shadowBorder.Effect = new DropShadowEffect
                    {
                        BlurRadius = appSettings.LeftSidebarGridShadow.BlurRadius,
                        Opacity = appSettings.LeftSidebarGridShadow.Opacity,
                        ShadowDepth = appSettings.LeftSidebarGridShadow.ShadowDepth,
                        Color = (Color)
                            ColorConverter.ConvertFromString(
                                appSettings.LeftSidebarGridShadow.ColorHex
                            ),
                    };
                }

                // 3. Terapkan Background pada Layer3Border
                if (
                    this.FindName("Layer3Border") is Border layer3Border
                    && appSettings.Layer3BorderBackground != null
                )
                {
                    var color = (Color)
                        ColorConverter.ConvertFromString(
                            appSettings.Layer3BorderBackground.ColorHex
                        );
                    layer3Border.Background = new SolidColorBrush(color);
                }

                // 4. Terapkan DropShadow ke CapsuleStyle Resource
                if (
                    this.Resources["CapsuleStyle"] is Style capsuleStyle
                    && appSettings.CapsuleStyleShadow != null
                )
                {
                    var shadow = new DropShadowEffect
                    {
                        BlurRadius = appSettings.CapsuleStyleShadow.BlurRadius,
                        Opacity = appSettings.CapsuleStyleShadow.Opacity,
                        ShadowDepth = appSettings.CapsuleStyleShadow.ShadowDepth,
                        Color = (Color)
                            ColorConverter.ConvertFromString(
                                appSettings.CapsuleStyleShadow.ColorHex
                            ),
                    };

                    var effectSetter = capsuleStyle
                        .Setters.OfType<Setter>()
                        .FirstOrDefault(s => s.Property == Border.EffectProperty);
                    if (effectSetter != null)
                    {
                        capsuleStyle.Setters.Remove(effectSetter);
                    }
                    capsuleStyle.Setters.Add(new Setter(Border.EffectProperty, shadow));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal menerapkan UI settings: " + ex.Message);
            }
        }

        private void SaveAllSettings()
        {
            appSettings.CustomShortcuts = customShortcuts;
            SettingsStorage.SaveSettings(appSettings);
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

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
                        bool isDuplicate = customShortcuts.Any(s =>
                            s.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
                        );
                        if (!isDuplicate)
                        {
                            string cleanTitle = GetFriendlyNameFromPath(path);
                            customShortcuts.Add(
                                new ShortcutItem { Title = cleanTitle, Path = path }
                            );
                            hasNewItems = true;
                        }
                    }

                    if (hasNewItems)
                    {
                        SaveAllSettings();
                        if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                        {
                            listControl.ItemsSource = null;
                            listControl.ItemsSource = customShortcuts;
                        }
                    }
                }
            }
        }

        private string GetFriendlyNameFromPath(string path)
        {
            try
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = path.TrimEnd(
                        System.IO.Path.DirectorySeparatorChar,
                        System.IO.Path.AltDirectorySeparatorChar
                    );
                }
                return name;
            }
            catch
            {
                return path;
            }
        }

        private void BtnDeleteShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is ShortcutItem itemToRemove)
            {
                customShortcuts.Remove(itemToRemove);
                SaveAllSettings();

                if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                {
                    listControl.ItemsSource = null;
                    listControl.ItemsSource = customShortcuts;
                }
            }
        }

        #endregion

        #region ==================== WIN32 EVENT HOOKS & ATTACHMENT ====================

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
            if (idObject != OBJID_WINDOW || hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return;

            if (eventType == EVENT_OBJECT_LOCATIONCHANGE && hwnd == targetExplorerHwnd)
            {
                SyncOverlayPositionInternalDirect();
                UpdateExplorerFocusState(targetExplorerHwnd);
                return;
            }

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

            if (eventType == EVENT_OBJECT_NAMECHANGE && hwnd == targetExplorerHwnd)
            {
                SyncOverlayPosition();
                return;
            }

            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                UpdateExplorerFocusState(hwnd);

                if (targetExplorerHwnd == IntPtr.Zero || IsCabinetWindow(hwnd))
                {
                    CheckAndUpdateTargetExplorer(hwnd);
                }
            }
            else if (eventType == EVENT_OBJECT_SHOW)
            {
                if (targetExplorerHwnd == IntPtr.Zero || IsCabinetWindow(hwnd))
                {
                    CheckAndUpdateTargetExplorer(hwnd);
                }
            }
        }

        private void SyncOverlayPositionInternalDirect()
        {
            if (
                targetExplorerHwnd == IntPtr.Zero
                || !IsWindow(targetExplorerHwnd)
                || !IsWindowVisible(targetExplorerHwnd)
                || !IsTargetExplorerProcessAlive(targetExplorerHwnd)
            )
            {
                DetachAndHide();
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
                    SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOCOPYBITS | SWP_DEFERERASE
                );
            }
        }

        private void CheckAndUpdateTargetExplorer(IntPtr foregroundHwnd)
        {
            if (IsCabinetWindow(foregroundHwnd) && IsWindowVisible(foregroundHwnd))
            {
                if (targetExplorerHwnd != foregroundHwnd)
                    AttachToExplorer(foregroundHwnd);
                else
                    SyncOverlayPosition();
            }
            else
            {
                if (
                    targetExplorerHwnd != IntPtr.Zero
                    && (
                        !IsWindow(targetExplorerHwnd)
                        || !IsWindowVisible(targetExplorerHwnd)
                        || !IsTargetExplorerProcessAlive(targetExplorerHwnd)
                    )
                )
                {
                    DetachAndHide();
                }
            }
        }

        private bool IsTargetExplorerProcessAlive(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            try
            {
                uint processId = 0;
                GetWindowThreadProcessId(hwnd, out processId);
                if (processId == 0)
                    return false;

                using Process process = Process.GetProcessById((int)processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private void DetachAndHide()
        {
            targetExplorerHwnd = IntPtr.Zero;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, IntPtr.Zero);

            if (Visibility == Visibility.Visible)
                Hide();
        }

        private void TryFindAndAttachExplorer()
        {
            IntPtr fgHwnd = GetForegroundWindow();
            if (IsCabinetWindow(fgHwnd))
            {
                AttachToExplorer(fgHwnd);
                return;
            }

            if (
                targetExplorerHwnd != IntPtr.Zero
                && IsWindowVisible(targetExplorerHwnd)
                && IsCabinetWindow(targetExplorerHwnd)
            )
            {
                AttachToExplorer(targetExplorerHwnd);
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
                AttachToExplorer(foundHwnd);
            else
                DetachAndHide();
        }

        private bool IsCabinetWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            classNameBuffer.Clear();
            int length = GetClassName(hwnd, classNameBuffer, classNameBuffer.Capacity);
            return length > 0 && classNameBuffer.ToString() == "CabinetWClass";
        }

        private void AttachToExplorer(IntPtr explorerHwnd)
        {
            targetExplorerHwnd = explorerHwnd;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, targetExplorerHwnd);
            SyncOverlayPosition();

            UpdateExplorerFocusState(GetForegroundWindow());
        }

        #endregion

        #region ==================== WINDOW OVERLAY POSITION & SYNC ====================

        private void SyncOverlayPosition()
        {
            isSyncPending = true;
            if (syncThrottleTimer != null && !syncThrottleTimer.IsEnabled)
            {
                syncThrottleTimer.Start();
            }
        }

        private void SyncOverlayPositionInternal()
        {
            if (
                targetExplorerHwnd == IntPtr.Zero
                || !IsWindow(targetExplorerHwnd)
                || !IsWindowVisible(targetExplorerHwnd)
                || !IsTargetExplorerProcessAlive(targetExplorerHwnd)
            )
            {
                syncThrottleTimer?.Stop();
                DetachAndHide();
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
                    SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOCOPYBITS | SWP_DEFERERASE
                );

                IntPtr currentTarget = targetExplorerHwnd;
                if (isTitleRefreshQueued)
                    return;

                isTitleRefreshQueued = true;
                Task.Run(() =>
                {
                    try
                    {
                        if (currentTarget == IntPtr.Zero || !IsWindow(currentTarget))
                            return;

                        string folderName = GetExplorerFolderNameThrottled(currentTarget);
                        string normalizedName = FormatFolderDisplayName(folderName);

                        Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                isTitleRefreshQueued = false;

                                if (
                                    currentTarget == targetExplorerHwnd
                                    && TxtTitle != null
                                    && !string.Equals(
                                        TxtTitle.Text,
                                        normalizedName,
                                        StringComparison.Ordinal
                                    )
                                )
                                {
                                    TxtTitle.Text = normalizedName;
                                }
                            })
                        );
                    }
                    catch
                    {
                        isTitleRefreshQueued = false;
                    }
                });
            }

            if (!isSyncPending)
            {
                syncThrottleTimer?.Stop();
            }
        }

        private string GetExplorerFolderName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return "Explorer";

            StringBuilder sbTitle = new StringBuilder(256);
            int length = GetWindowText(hwnd, sbTitle, sbTitle.Capacity);

            if (length <= 0)
                return "Explorer";

            string rawTitle = sbTitle.ToString().Trim();
            if (string.IsNullOrWhiteSpace(rawTitle))
                return "Explorer";

            string cleanTitle = TabSuffixRegex
                .Replace(rawTitle, "")
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
                string displayName = !string.IsNullOrWhiteSpace(extractedName)
                    ? extractedName
                    : cleanTitle;

                return FormatFolderDisplayName(displayName);
            }
            catch
            {
                return FormatFolderDisplayName(cleanTitle);
            }
        }

        private string FormatFolderDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Explorer";

            if (name.Length <= MAX_FOLDER_TITLE_LENGTH)
                return name;

            return name.Substring(0, MAX_FOLDER_TITLE_LENGTH - 1) + "…";
        }

        private string GetExplorerFolderNameThrottled(IntPtr hwnd)
        {
            long currentMs = pathCheckStopwatch.ElapsedMilliseconds;

            if (currentMs - lastPathCheckMs < PATH_CHECK_THROTTLE_MS)
                return lastKnownFolderName;

            lastPathCheckMs = currentMs;
            lastKnownFolderName = GetExplorerFolderName(hwnd);
            return lastKnownFolderName;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                {
                    SetForegroundWindow(targetExplorerHwnd);
                }
                handled = true;
                return (IntPtr)MA_NOACTIVATE;
            }
            return IntPtr.Zero;
        }

        #endregion

        #region ==================== WINDOW CONTROL & COMMANDS ====================

        private void SendKeysToExplorer(string keys)
        {
            if (targetExplorerHwnd == IntPtr.Zero || !IsWindow(targetExplorerHwnd))
                return;

            try
            {
                SetForegroundWindow(targetExplorerHwnd);
                wshellInstance?.SendKeys(keys);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Focus/SendKeys error: " + ex.Message);
            }
        }

        private async Task SendKeysToExplorerAsync(string keys, int delayMs = 30)
        {
            if (targetExplorerHwnd == IntPtr.Zero || !IsWindow(targetExplorerHwnd))
                return;

            try
            {
                SetForegroundWindow(targetExplorerHwnd);
                if (delayMs > 0)
                    await Task.Delay(delayMs);

                wshellInstance?.SendKeys(keys);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SendKeys async error: " + ex.Message);
            }
        }

        private bool isNavigating = false;

        private async void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (isNavigating)
                return;

            if (
                sender is Button btn
                && btn.Tag is string pathTarget
                && !string.IsNullOrWhiteSpace(pathTarget)
                && targetExplorerHwnd != IntPtr.Zero
            )
            {
                isNavigating = true;
                string targetPath = pathTarget;

                if (Enum.TryParse(pathTarget, out Environment.SpecialFolder folder))
                {
                    targetPath = Environment.GetFolderPath(folder);
                }

                string escapedPath = EscapeSendKeysString(targetPath);

                try
                {
                    await SendKeysToExplorerAsync("^l", 50);
                    await Task.Delay(100);

                    await SendKeysToExplorerAsync(escapedPath + "{ENTER}", 20);
                    await Task.Delay(500);
                }
                catch { }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        private string EscapeSendKeysString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            StringBuilder sb = new StringBuilder(text.Length * 2);

            foreach (char c in text)
            {
                switch (c)
                {
                    case '{':
                        sb.Append("{{}");
                        break;
                    case '}':
                        sb.Append("{}}");
                        break;
                    case '(':
                        sb.Append("{(}");
                        break;
                    case ')':
                        sb.Append("{)}");
                        break;
                    case '+':
                        sb.Append("{+}");
                        break;
                    case '^':
                        sb.Append("{^}");
                        break;
                    case '%':
                        sb.Append("{%}");
                        break;
                    case '~':
                        sb.Append("{~}");
                        break;
                    case '[':
                        sb.Append("{[}");
                        break;
                    case ']':
                        sb.Append("{]}");
                        break;
                    case '"':
                        sb.Append("{\"}\"");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("%{LEFT}");

        private void BtnForward_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("%{RIGHT}");

        private void BtnUp_Click(object sender, RoutedEventArgs e) => SendKeysToExplorer("%{UP}");

        private void BtnViewIcons_Click(object sender, RoutedEventArgs e) =>
            SendKeysToExplorer("^+3");

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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                SendMessage(targetExplorerHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                SendMessage(targetExplorerHwnd, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero);
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
            {
                SendMessage(
                    targetExplorerHwnd,
                    WM_SYSCOMMAND,
                    IsZoomed(targetExplorerHwnd) ? (IntPtr)SC_RESTORE : (IntPtr)SC_MAXIMIZE,
                    IntPtr.Zero
                );
            }
        }

        #endregion

        #region ==================== GRAPHICS & PARALLAX EFFECT ====================

        private BitmapSource AdjustContrastAndSaturation(
            BitmapSource original,
            float contrast,
            float saturation
        )
        {
            int width = original.PixelWidth;
            int height = original.PixelHeight;

            FormatConvertedBitmap converted = new FormatConvertedBitmap(
                original,
                PixelFormats.Bgra32,
                null,
                0
            );
            WriteableBitmap wBitmap = new WriteableBitmap(converted);

            float contrastFactor =
                (259f * (contrast * 255f + 255f)) / (255f * (259f - contrast * 255f));

            wBitmap.Lock();
            unsafe
            {
                byte* ptr = (byte*)wBitmap.BackBuffer.ToPointer();
                int stride = wBitmap.BackBufferStride;
                int totalBytes = height * stride;

                for (int i = 0; i < totalBytes; i += 4)
                {
                    float b = ptr[i] / 255f;
                    float g = ptr[i + 1] / 255f;
                    float r = ptr[i + 2] / 255f;

                    float gray = 0.299f * r + 0.587f * g + 0.114f * b;

                    if (gray <= 0.15f)
                    {
                        r = 1.0f;
                        g = 1.0f;
                        b = 1.0f;
                    }
                    else
                    {
                        r = (contrastFactor * (r * 255f - 128f) + 128f) / 255f;
                        g = (contrastFactor * (g * 255f - 128f) + 128f) / 255f;
                        b = (contrastFactor * (b * 255f - 128f) + 128f) / 255f;

                        float newGray = 0.299f * r + 0.587f * g + 0.114f * b;
                        r = newGray + (r - newGray) * saturation;
                        g = newGray + (g - newGray) * saturation;
                        b = newGray + (b - newGray) * saturation;
                    }

                    ptr[i] = (byte)(Math.Max(0, Math.Min(255, b * 255f)));
                    ptr[i + 1] = (byte)(Math.Max(0, Math.Min(255, g * 255f)));
                    ptr[i + 2] = (byte)(Math.Max(0, Math.Min(255, r * 255f)));
                }

                wBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            wBitmap.Unlock();
            wBitmap.Freeze();
            return wBitmap;
        }

        private async Task LoadDesktopWallpaperAsync()
        {
            try
            {
                BitmapSource? processedBitmap = await Task.Run(() =>
                {
                    StringBuilder wallPaperPath = new StringBuilder(260);
                    SystemParametersInfo(
                        SPI_GETDESKWALLPAPER,
                        (uint)wallPaperPath.Capacity,
                        wallPaperPath,
                        0
                    );

                    string path = wallPaperPath.ToString();
                    if (!File.Exists(path))
                        return null;

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 1024;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    BitmapSource result = AdjustContrastAndSaturation(
                        bitmap,
                        contrast: 0.65f,
                        saturation: 2.1f
                    );
                    if (result.CanFreeze)
                        result.Freeze();

                    return result;
                });

                if (processedBitmap != null && ParallaxCanvas != null)
                {
                    wallpaperBrush = new ImageBrush(processedBitmap)
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

        #endregion

        #region ==================== UI ANIMATIONS & FOCUS HANDLERS ====================
        /// <summary>
        /// Animasi perubahan Opacity pada FrameworkElement secara mulus
        /// </summary>
        private void AnimateOpacity(FrameworkElement element, double targetOpacity)
        {
            if (element == null)
                return;

            // Clamp nilai opacity antara 0.0 - 1.0
            double clampedOpacity = Math.Max(0.0, Math.Min(1.0, targetOpacity));

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                To = clampedOpacity,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };

            element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private SolidColorBrush CreateBrushWithOpacity(BrushSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(settings.ColorHex))
                return new SolidColorBrush(Colors.Transparent);

            Color baseColor = (Color)ColorConverter.ConvertFromString(settings.ColorHex);

            // Jika user memasukkan hex 6 digit (#FFFFFF), paksa Alpha ke 255 (Full Solid)
            baseColor.A = 255;

            return new SolidColorBrush(baseColor);
        }

        private void UpdateExplorerFocusState(IntPtr activeHwnd)
        {
            if (isFocusStateRefreshQueued)
                return;

            isFocusStateRefreshQueued = true;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    isFocusStateRefreshQueued = false;

                    IntPtr currentForeground = GetForegroundWindow();
                    bool isExplorerFocused = (
                        targetExplorerHwnd != IntPtr.Zero
                        && currentForeground == targetExplorerHwnd
                        && IsWindowVisible(targetExplorerHwnd)
                    );

                    if (isExplorerFocused == lastExplorerFocusVisualState)
                        return;

                    lastExplorerFocusVisualState = isExplorerFocused;

                    var dotClose = this.FindName("DotClose") as FrameworkElement;
                    var dotMinimize = this.FindName("DotMinimize") as FrameworkElement;
                    var dotMaximize = this.FindName("DotMaximize") as FrameworkElement;
                    var layer3Border = this.FindName("Layer3Border") as Border;

                    if (isExplorerFocused)
                    {
                        if (dotClose != null)
                            AnimateBackground(dotClose, "#FF5F56");
                        if (dotMinimize != null)
                            AnimateBackground(dotMinimize, "#FFBD2E");
                        if (dotMaximize != null)
                            AnimateBackground(dotMaximize, "#27C93F");
                        if (layer3Border != null)
                        {
                            // SAAT FOKUS: Gunakan nilai Opacity murni dari setting JSON (misal: 0.8)
                            double targetOpacity =
                                appSettings.Layer3BorderBackground?.Opacity ?? 0.8;
                            AnimateOpacity(layer3Border, targetOpacity);
                        }
                    }
                    else
                    {
                        if (dotClose != null)
                            AnimateBackground(dotClose, "#D0D0D0");
                        if (dotMinimize != null)
                            AnimateBackground(dotMinimize, "#D0D0D0");
                        if (dotMaximize != null)
                            AnimateBackground(dotMaximize, "#D0D0D0");
                        if (layer3Border != null)
                        {
                            AnimateOpacity(layer3Border, 1.0);
                        }
                    }
                }),
                DispatcherPriority.Render
            );
        }

        private void AnimateBackground(FrameworkElement element, string targetHexColor)
        {
            if (element == null)
                return;

            Color targetColor = (Color)ColorConverter.ConvertFromString(targetHexColor);
            SolidColorBrush? currentBrush = null;

            if (element is Button btn)
                currentBrush = btn.Background as SolidColorBrush;
            else if (element is Border border)
                currentBrush = border.Background as SolidColorBrush;

            if (currentBrush == null || currentBrush.IsFrozen)
            {
                currentBrush = new SolidColorBrush(Colors.Transparent);
                if (element is Button btnTarget)
                    btnTarget.Background = currentBrush;
                else if (element is Border borderTarget)
                    borderTarget.Background = currentBrush;
            }

            Color currentColor = currentBrush.Color;

            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = currentColor,
                To = targetColor,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };

            currentBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void Sidebar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (
                e.OriginalSource is FrameworkElement element
                && element.ContextMenu != null
                && element.ContextMenu != (sender as FrameworkElement)?.ContextMenu
            )
            {
                e.Handled = true;
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Dapatkan path lengkap file app_settings.json di lokasi jalannya executable (.exe)
                string settingsPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "app_settings.json"
                );

                // 2. Jika file belum ada, buat file default dulu agar tidak error saat dibuka
                if (!System.IO.File.Exists(settingsPath))
                {
                    SaveAllSettings(); // Memanggil fungsi simpan yang sudah kamu buat sebelumnya
                }

                // 3. Jalankan file menggunakan aplikasi default sistem (Shell)
                Process.Start(
                    new ProcessStartInfo { FileName = settingsPath, UseShellExecute = true }
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal membuka file pengaturan: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnReloadJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reloadedSettings = SettingsStorage.LoadSettings();
                appSettings = reloadedSettings;
                customShortcuts =
                    appSettings.CustomShortcuts ?? new ObservableCollection<ShortcutItem>();

                if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                {
                    listControl.ItemsSource = null;
                    listControl.ItemsSource = customShortcuts;
                }

                ApplyAppSettingsToUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal reload JSON settings: " + ex.Message);
            }
        }

        private void BtnRelaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    SaveAllSettings();
                    Process.Start(
                        new ProcessStartInfo { FileName = exePath, UseShellExecute = true }
                    );
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to relaunch application: " + ex.Message);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            SaveAllSettings();
            Application.Current.Shutdown();
        }

        #endregion

        #region ==================== CLEANUP & DISPOSAL ====================

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (syncThrottleTimer != null)
            {
                syncThrottleTimer.Stop();
                syncThrottleTimer = null;
            }

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

            if (wshellInstance != null)
            {
                Marshal.ReleaseComObject(wshellInstance);
                wshellInstance = null;
            }
        }

        #endregion
    }
}
