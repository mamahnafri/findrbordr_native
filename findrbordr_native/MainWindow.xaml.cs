using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

        // --- Tambahkan Field Baru pada MainWindow ---
        private DispatcherTimer? syncThrottleTimer;
        private bool isSyncPending = false;
        private bool updatePending = false;
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
        public string UserProfileName { get; set; }

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
                        // 1. Cek duplikasi path (Case-insensitive)
                        bool isDuplicate = customShortcuts.Any(s =>
                            s.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
                        );

                        if (!isDuplicate)
                        {
                            // 2. Ekstrak nama judul secara bersih (Hilangkan .lnk jika ada)
                            string cleanTitle = GetFriendlyNameFromPath(path);

                            // 3. Tambahkan ke koleksi
                            customShortcuts.Add(
                                new ShortcutItem { Title = cleanTitle, Path = path }
                            );

                            hasNewItems = true;
                        }
                    }

                    if (hasNewItems)
                    {
                        // Simpan ke storage
                        ShortcutStorage.SaveShortcuts(customShortcuts);

                        // Jika customShortcuts menggunakan ObservableCollection,
                        // blok Dispatcher/ItemsSource di bawah ini SEBENARNYA TIDAK DIPERLUKAN LAGI.
                        // Tapi jika masih List biasa, tetap gunakan pembaharuan ini:
                        if (this.FindName("CustomShortcutsList") is ItemsControl listControl)
                        {
                            listControl.ItemsSource = null;
                            listControl.ItemsSource = customShortcuts;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper untuk membersihkan nama file dari ekstensi .lnk atau format path root
        /// </summary>
        private string GetFriendlyNameFromPath(string path)
        {
            try
            {
                // Ambil nama file tanpa ekstensi (misal "Chrome.lnk" -> "Chrome")
                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (string.IsNullOrWhiteSpace(name))
                {
                    // Jika root drive misal "C:\", jadikan "C: Drive" atau tetap "C:\"
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
                    // Sebelum relaunch, pastikan jika ada konfigurasi di memori yang belum tersimpan, simpan dulu!
                    // SaveMySettings();

                    // Jalankan proses baru
                    Process.Start(
                        new ProcessStartInfo { FileName = exePath, UseShellExecute = true }
                    );

                    // Langsung matikan proses saat ini secara tegas
                    Environment.Exit(0); // Menggunakan Exit(0) lebih instan dibanding Shutdown() untuk relaunch
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

            // --- Inisialisasi DispatcherTimer Throttling Sync Overlay (~60 FPS) ---
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

            //get username
            UserProfileName = Environment.UserName;
            // Set DataContext ke dirinya sendiri agar XAML bisa membaca properti
            this.DataContext = this;

            // Memastikan setiap kali area WPF ditekan, Explorer langsung fokus
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
            // Jalankan wallpaper loader secara asynchronous
            await LoadDesktopWallpaperAsync();

            // Update posisi parallax setelah wallpaper siap
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
            base.OnClosed(e);

            // Hentikan Throttle Timer
            if (syncThrottleTimer != null)
            {
                syncThrottleTimer.Stop();
                syncThrottleTimer = null;
            }

            // Cleanup hooks yang sudah ada sebelumnya
            if (locationHook != IntPtr.Zero)
            {
                UnhookWinEvent(locationHook);
                locationHook = IntPtr.Zero;
            }
            if (foregroundHook != IntPtr.Zero)
            {
                UnhookWinEvent(foregroundHook);
                foregroundHook = IntPtr.Zero;
            }
            if (destroyHook != IntPtr.Zero)
            {
                UnhookWinEvent(destroyHook);
                destroyHook = IntPtr.Zero;
            }
            if (showHook != IntPtr.Zero)
            {
                UnhookWinEvent(showHook);
                showHook = IntPtr.Zero;
            }
            if (nameHook != IntPtr.Zero)
            {
                UnhookWinEvent(nameHook);
                nameHook = IntPtr.Zero;
            }

            if (wshellInstance != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wshellInstance);
                wshellInstance = null;
            }
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

            // Filter: hanya proses jika hwnd valid
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return;

            // 1. Jika Explorer ditutup / tersembunyi
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

            // 2. Jika ada perubahan jendela aktif di Windows (Foreground Event)
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                // Panggil fungsi pembaru status fokus (Aktif / Inaktif)
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
            else if (
                (eventType == EVENT_OBJECT_LOCATIONCHANGE || eventType == EVENT_OBJECT_NAMECHANGE)
                && hwnd == targetExplorerHwnd
            )
            {
                SyncOverlayPosition();
            }
        }

        // 1. Variabel pendukung Throttling (Taruh di level Class)
        private string lastKnownFolderName = "Explorer";
        private readonly Stopwatch pathCheckStopwatch = Stopwatch.StartNew();
        private long lastPathCheckMs = -1000;
        private const int PATH_CHECK_THROTTLE_MS = 300; // Throttle 300ms

        // 2. Helper Method Ekstraksi Judul (Dibuat method tersendiri)
        private static readonly Regex TabSuffixRegex = new Regex(
            @" and \d+ more tabs",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private string GetExplorerFolderName(IntPtr hwnd)
        {
            // Guard Clause Handle: Cegah P/Invoke ke API Win32 jika handle bernilai IntPtr.Zero atau sudah hancur
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return "Explorer";

            StringBuilder sbTitle = new StringBuilder(256);
            int length = GetWindowText(hwnd, sbTitle, sbTitle.Capacity);

            // Jika gagal mendapatkan text/titlenya kosong
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
                return !string.IsNullOrWhiteSpace(extractedName) ? extractedName : cleanTitle;
            }
            catch
            {
                return cleanTitle;
            }
        }

        // 3. Wrapper Throttling
        private string GetExplorerFolderNameThrottled(IntPtr hwnd)
        {
            long currentMs = pathCheckStopwatch.ElapsedMilliseconds;

            // Jika dipanggil terlalu sering (kurang dari 300ms), pakai hasil terakhir (cache)
            if (currentMs - lastPathCheckMs < PATH_CHECK_THROTTLE_MS)
            {
                return lastKnownFolderName;
            }

            lastPathCheckMs = currentMs;
            lastKnownFolderName = GetExplorerFolderName(hwnd);
            return lastKnownFolderName;
        }

        private void UpdateExplorerFocusState(IntPtr activeHwnd)
        {
            // Cek apakah jendela yang aktif sekarang adalah Explorer target
            bool isExplorerFocused = (
                targetExplorerHwnd != IntPtr.Zero && activeHwnd == targetExplorerHwnd
            );

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    // Cari elemen Mac Dots dan Layer3Border jika x:Name di XAML sudah dipasang
                    var dotClose = this.FindName("DotClose") as FrameworkElement;
                    var dotMinimize = this.FindName("DotMinimize") as FrameworkElement;
                    var dotMaximize = this.FindName("DotMaximize") as FrameworkElement;
                    var layer3Border = this.FindName("Layer3Border") as Border;

                    if (isExplorerFocused)
                    {
                        // 🟢 EXPLORER FOKUS: Tampilkan Warna Khas macOS & Kaca Transparan
                        if (dotClose != null)
                            AnimateBackground(dotClose, "#FF5F56");
                        if (dotMinimize != null)
                            AnimateBackground(dotMinimize, "#FFBD2E");
                        if (dotMaximize != null)
                            AnimateBackground(dotMaximize, "#27C93F");
                        if (layer3Border != null)
                            AnimateBackground(layer3Border, "#E6FFFFFF");
                    }
                    else
                    {
                        // ⚪ EXPLORER UNFOCUSED: Animasi Fade-Out ke Abu-abu & Solid White
                        if (dotClose != null)
                            AnimateBackground(dotClose, "#D0D0D0");
                        if (dotMinimize != null)
                            AnimateBackground(dotMinimize, "#D0D0D0");
                        if (dotMaximize != null)
                            AnimateBackground(dotMaximize, "#D0D0D0");
                        if (layer3Border != null)
                            AnimateBackground(layer3Border, "#FFFFFFFF");
                    }
                }),
                DispatcherPriority.Render
            );
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
            // 1. Cek dulu jendela aktif (Paling sering & paling cepat - 0ms)
            IntPtr fgHwnd = GetForegroundWindow();
            if (IsCabinetWindow(fgHwnd))
            {
                AttachToExplorer(fgHwnd);
                return;
            }

            // 2. Cek apakah target Explorer lama kita masih hidup & terlihat (Mencegah EnumWindows)
            if (
                targetExplorerHwnd != IntPtr.Zero
                && IsWindowVisible(targetExplorerHwnd)
                && IsCabinetWindow(targetExplorerHwnd)
            )
            {
                AttachToExplorer(targetExplorerHwnd);
                return;
            }

            // 3. Fallback: Cari jendela Explorer lain hanya jika benar-benar perlu
            IntPtr foundHwnd = IntPtr.Zero;
            EnumWindows(
                (hwnd, lParam) =>
                {
                    if (IsWindowVisible(hwnd) && IsCabinetWindow(hwnd))
                    {
                        foundHwnd = hwnd;
                        return false; // Stop iterasi segera setelah ketemu 1
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
                DetachAndHide(); // Atau Hide()
            }
        }

        // Di level class
        private readonly StringBuilder classNameBuffer = new StringBuilder(256);

        private bool IsCabinetWindow(IntPtr hwnd)
        {
            // 1. Cek validitas handle dulu (mencegah panggilan Win32 API sia-sia)
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            // 2. Bersihkan buffer tanpa alokasi memori baru
            classNameBuffer.Clear();

            // 3. Panggil API & pastikan return length > 0
            int length = GetClassName(hwnd, classNameBuffer, classNameBuffer.Capacity);

            return length > 0 && classNameBuffer.ToString() == "CabinetWClass";
        }

        private void AttachToExplorer(IntPtr explorerHwnd)
        {
            targetExplorerHwnd = explorerHwnd;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, targetExplorerHwnd);
            SyncOverlayPosition();

            // Pemicu warna langsung begitu berhasil ditempel
            UpdateExplorerFocusState(GetForegroundWindow());
        }

        private void ScheduleUpdate(Action action)
        {
            // Jika sudah ada jadwal update yang mengantre, abaikan panggilan baru
            if (updatePending)
                return;

            updatePending = true;
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    action();
                    updatePending = false; // Reset status setelah selesai dieksekusi
                }),
                DispatcherPriority.Render
            );
        }

        // Method publik/eksternal kini bertindak sebagai throttler
        private void SyncOverlayPosition()
        {
            isSyncPending = true;
            if (syncThrottleTimer != null && !syncThrottleTimer.IsEnabled)
            {
                syncThrottleTimer.Start();
            }
        }

        // Logika aktual pembaruan posisi DWM dimasukkan ke fungsi internal
        private void SyncOverlayPositionInternal()
        {
            // Guard Clause Handle: Hentikan timer & sembunyikan jika HWND hilang/hancur
            if (
                targetExplorerHwnd == IntPtr.Zero
                || !IsWindow(targetExplorerHwnd)
                || !IsWindowVisible(targetExplorerHwnd)
            )
            {
                syncThrottleTimer?.Stop();
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

                // Task runner title dengan guard check
                IntPtr currentTarget = targetExplorerHwnd;
                Task.Run(() =>
                {
                    // Pastikan handle masih valid sebelum mengeksekusi P/Invoke GetWindowText
                    if (currentTarget == IntPtr.Zero || !IsWindow(currentTarget))
                        return;

                    string folderName = GetExplorerFolderNameThrottled(currentTarget);

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

            // Hentikan timer jika tidak ada queue pergerakan lanjutan
            if (!isSyncPending)
            {
                syncThrottleTimer?.Stop();
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

        private async void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (
                sender is Button btn
                && btn.Tag is string pathTarget
                && !string.IsNullOrWhiteSpace(pathTarget)
                && targetExplorerHwnd != IntPtr.Zero
            )
            {
                string targetPath = pathTarget;

                if (Enum.TryParse(pathTarget, out Environment.SpecialFolder folder))
                {
                    targetPath = Environment.GetFolderPath(folder);
                }

                string escapedPath = EscapeSendKeysString(targetPath);

                try
                {
                    // 1. Paksa Window Explorer target fokus ke depan lebih dulu
                    SetForegroundWindow(targetExplorerHwnd);
                    await Task.Delay(50); // Jeda singkat agar fokus Windows OS berpindah

                    // 2. Kirim Ctrl + L ke tab yang SEDANG AKTIF di Explorer tersebut
                    SendKeysToExplorer("^l");

                    // 3. Jeda sedikit agar Address Bar ter-highlight sempurna
                    await Task.Delay(80);

                    // 4. Ketik path baru + Enter
                    SendKeysToExplorer(escapedPath + "{ENTER}");
                }
                catch (Exception ex)
                {
                    // Logging / Error handling
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
                    // Karakter khusus SendKeys yang WAJIB di-escape
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

                    // Tambahan karakter riskan agar lebih aman
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
            {
                SendMessage(targetExplorerHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
            {
                SendMessage(targetExplorerHwnd, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero);
            }
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

        // =========================================================================
        // 1. OPTIMASI MEMORI: AdjustContrastAndSaturation (Zero Allocation + Unsafe)
        // =========================================================================
        private BitmapSource AdjustContrastAndSaturation(
            BitmapSource original,
            float contrast,
            float saturation
        )
        {
            int width = original.PixelWidth;
            int height = original.PixelHeight;

            // Konversi gambar ke format Bgra32
            FormatConvertedBitmap converted = new FormatConvertedBitmap(
                original,
                PixelFormats.Bgra32,
                null,
                0
            );

            WriteableBitmap wBitmap = new WriteableBitmap(converted);

            // Faktor pengali kontras
            float contrastFactor =
                (259f * (contrast * 255f + 255f)) / (255f * (259f - contrast * 255f));

            // 🟢 GUNAKAN UNSAFE POINTER: Membaca & menulis memori piksel secara LANGSUNG
            // tanpa mengalokasikan byte[] array baru di RAM (0 Garbage Allocation!)
            wBitmap.Lock();
            unsafe
            {
                byte* ptr = (byte*)wBitmap.BackBuffer.ToPointer();
                int stride = wBitmap.BackBufferStride;
                int totalBytes = height * stride;

                for (int i = 0; i < totalBytes; i += 4)
                {
                    // Akses memori BGRA secara langsung via pointer offset
                    float b = ptr[i] / 255f;
                    float g = ptr[i + 1] / 255f;
                    float r = ptr[i + 2] / 255f;

                    // Hitung kecerahan awal piksel (Luminance)
                    float gray = 0.299f * r + 0.587f * g + 0.114f * b;

                    // Ambang batas area gelap
                    if (gray <= 0.15f)
                    {
                        r = 1.0f;
                        g = 1.0f;
                        b = 1.0f;
                    }
                    else
                    {
                        // Formula Kontras
                        r = (contrastFactor * (r * 255f - 128f) + 128f) / 255f;
                        g = (contrastFactor * (g * 255f - 128f) + 128f) / 255f;
                        b = (contrastFactor * (b * 255f - 128f) + 128f) / 255f;

                        // Formula Saturasi
                        float newGray = 0.299f * r + 0.587f * g + 0.114f * b;
                        r = newGray + (r - newGray) * saturation;
                        g = newGray + (g - newGray) * saturation;
                        b = newGray + (b - newGray) * saturation;
                    }

                    // Tulis kembali nilai ke memori piksel (Clamp 0-255)
                    ptr[i] = (byte)(Math.Max(0, Math.Min(255, b * 255f))); // Blue
                    ptr[i + 1] = (byte)(Math.Max(0, Math.Min(255, g * 255f))); // Green
                    ptr[i + 2] = (byte)(Math.Max(0, Math.Min(255, r * 255f))); // Red
                    // ptr[i + 3] adalah Alpha (tetap)
                }

                wBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            wBitmap.Unlock();

            wBitmap.Freeze(); // Freeze agar thread-safe dan hemat RAM
            return wBitmap;
        }

        private async Task LoadDesktopWallpaperAsync()
        {
            try
            {
                // 1. DECODING & OLAH GAMBAR DI BACKGROUND THREAD
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

                    // A. Decode Gambar
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 1024;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Wajib Freeze agar bisa diolah di fungsi bawah

                    // B. Olah Kontras & Saturasi (Proses paling berat)
                    BitmapSource result = AdjustContrastAndSaturation(
                        bitmap,
                        contrast: 0.65f,
                        saturation: 2.1f
                    );

                    // C. Freeze hasil Bitmap-nya agar aman dikirim ke UI Thread
                    if (result.CanFreeze)
                    {
                        result.Freeze();
                    }

                    return result;
                });

                // 2. PEMBUATAN BRUSH DI UI THREAD (Di luar Task.Run)
                if (processedBitmap != null && ParallaxCanvas != null)
                {
                    // Buat ImageBrush langsung di UI Thread menggunakan bitmap yang sudah di-freeze
                    wallpaperBrush = new ImageBrush(processedBitmap)
                    {
                        Stretch = Stretch.Fill,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        ViewportUnits = BrushMappingMode.Absolute,
                        RelativeTransform = new ScaleTransform(1.1, 1.1, 0.5, 0.5),
                    };

                    // Pasang ke UI
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

        // 🟢 SAAT WINDOW FOKUS / AKTIF (Fade In ke Warna Asli)
        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            AnimateBackground(DotClose, "#FF5F56");
            AnimateBackground(DotMinimize, "#FFBD2E");
            AnimateBackground(DotMaximize, "#27C93F");

            AnimateBackground(Layer3Border, "#E6FFFFFF");
        }

        // ⚪ SAAT WINDOW UNFOCUSED / NON-AKTIF (Fade Out ke Abu-abu & Solid White)
        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            AnimateBackground(DotClose, "#D0D0D0");
            AnimateBackground(DotMinimize, "#D0D0D0");
            AnimateBackground(DotMaximize, "#D0D0D0");

            AnimateBackground(Layer3Border, "#FFFFFFFF");
        }

        /// <summary>
        /// Helper universal untuk animasi transisi warna Background (Button, Border, dsb)
        /// </summary>
        private void AnimateBackground(FrameworkElement element, string targetHexColor)
        {
            if (element == null)
                return;

            Color targetColor = (Color)ColorConverter.ConvertFromString(targetHexColor);

            // 1. Ambil atau buat SolidColorBrush yang bisa dianimasikan
            SolidColorBrush? currentBrush = null;

            if (element is System.Windows.Controls.Button btn)
            {
                currentBrush = btn.Background as SolidColorBrush;
            }
            else if (element is System.Windows.Controls.Border border)
            {
                currentBrush = border.Background as SolidColorBrush;
            }

            // Jika Brush belum ada atau Frozen (tidak bisa dianimasikan), buat baru yang Mutable
            if (currentBrush == null || currentBrush.IsFrozen)
            {
                currentBrush = new SolidColorBrush(Colors.Transparent);

                if (element is System.Windows.Controls.Button btnTarget)
                    btnTarget.Background = currentBrush;
                else if (element is System.Windows.Controls.Border borderTarget)
                    borderTarget.Background = currentBrush;
            }

            // 2. Ambil warna saat ini sebagai titik start (agar transisi mulus meski ditengah animasi)
            Color currentColor = currentBrush.Color;

            // 3. Buat animasi
            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = currentColor,
                To = targetColor,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };

            // 4. Jalankan animasi langsung pada Brush yang SAMA (Tanpa 'new SolidColorBrush' terus-menerus)
            currentBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }
    }
}
