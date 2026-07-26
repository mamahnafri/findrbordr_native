namespace findrbordr_native
{
    public partial class MainWindow
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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(
            IntPtr hwndParent,
            IntPtr hwndChildAfter,
            string lpszClass,
            string? lpszWindow
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
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
        private const uint WS_EX_LAYERED = 0x00080000;

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
        private readonly StringBuilder sbTitle = new StringBuilder(256);

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
                SyncOverlayPositionFast();
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
                SyncOverlayPositionFast();
                RefreshTitleThrottled();
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
                if (!IsExplorerWindowValid())
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

        private bool IsExplorerWindowValid()
        {
            return targetExplorerHwnd != IntPtr.Zero
                && IsWindow(targetExplorerHwnd)
                && IsWindowVisible(targetExplorerHwnd)
                && IsTargetExplorerProcessAlive(targetExplorerHwnd);
        }

        private void DetachAndHide()
        {
            targetExplorerHwnd = IntPtr.Zero;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;

            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, IntPtr.Zero);
            ReleaseWallpaperResources();

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
            
            if (wallpaperBrush == null)
                _ = LoadDesktopWallpaperAsync();
            
            SyncOverlayPosition();
            UpdateParallaxOffset();

            UpdateExplorerFocusState(GetForegroundWindow());
        }

        #endregion
    }
}
