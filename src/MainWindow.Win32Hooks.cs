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

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        #endregion

        #region ==================== HELPER METHODS ====================

        private void ForceForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return;

            uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            uint currentThread = GetWindowThreadProcessId(cachedHwnd, out _);

            if (foregroundThread != currentThread)
            {
                AttachThreadInput((IntPtr)currentThread, (IntPtr)foregroundThread, true);
                SetForegroundWindow(hWnd);
                AttachThreadInput((IntPtr)currentThread, (IntPtr)foregroundThread, false);
            }
            else
            {
                SetForegroundWindow(hWnd);
            }

            ShowWindow(hWnd, SW_RESTORE);
        }

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
        private const uint EVENT_OBJECT_SELECTION = 0x8006;
        private const uint EVENT_OBJECT_SELECTIONWITHIN = 0x8007;
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
        private const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        private const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int OBJID_WINDOW = 0;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPARENT = 0x00000020;

        private bool isTitleRefreshQueued = false;
        private bool lastExplorerFocusVisualState = false;

        private IntPtr targetExplorerHwnd = IntPtr.Zero;
        private dynamic? wshellInstance;

        private readonly object _navItemsLock = new();
        private List<NavPaneItemModel> cachedNavItems = new();
        public ObservableCollection<NavPaneItemModel> NavPaneItems { get; set; } = new();
        private int isScanning = 0;
        private bool _isShuttingDown = false;
        private int cachedNavPaneWidth = 0;
        private int cachedExplorerTop = 0;
        private const double SIDEBAR_DEFAULT_WIDTH = 185;
        private bool isTransparent = false;
        private IntPtr cachedHwnd = IntPtr.Zero;
        private bool lastIsExplorerFocused = false;

        private const int VK_LBUTTON = 0x01;
        private System.Windows.Threading.DispatcherTimer? _hoverUpdateTimer;
        private System.Windows.Threading.DispatcherTimer? _navPaneScanTimer;

        private WinEventDelegate? winEventDelegate;
        private IntPtr locationHook = IntPtr.Zero;
        private IntPtr foregroundHook = IntPtr.Zero;
        private IntPtr destroyHook = IntPtr.Zero;
        private IntPtr showHook = IntPtr.Zero;
        private IntPtr nameHook = IntPtr.Zero;
        private IntPtr moveSizeStartHook = IntPtr.Zero;
        private IntPtr moveSizeEndHook = IntPtr.Zero;
        private IntPtr selectionHook = IntPtr.Zero;
        private bool isExplorerBeingDragged = false;
        private long lastPositionSyncTick = 0;
        private const int POSITION_SYNC_THROTTLE_MS = 50;

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
            moveSizeStartHook = SetWinEventHook(
                EVENT_SYSTEM_MOVESIZESTART,
                EVENT_SYSTEM_MOVESIZESTART,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );
            moveSizeEndHook = SetWinEventHook(
                EVENT_SYSTEM_MOVESIZEEND,
                EVENT_SYSTEM_MOVESIZEEND,
                IntPtr.Zero,
                winEventDelegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT
            );
            selectionHook = SetWinEventHook(
                EVENT_OBJECT_SELECTION,
                EVENT_OBJECT_SELECTIONWITHIN,
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

            // Prevent re-entrancy
            if (_isShuttingDown)
                return;

            // Handle drag start - fade out overlay immediately
            if (eventType == EVENT_SYSTEM_MOVESIZESTART && hwnd == targetExplorerHwnd)
            {
                isExplorerBeingDragged = true;
                Dispatcher.BeginInvoke(new Action(() => FadeOutOverlay()));
                return;
            }

            // Handle drag end - fade in overlay and recalculate
            if (eventType == EVENT_SYSTEM_MOVESIZEEND && hwnd == targetExplorerHwnd)
            {
                isExplorerBeingDragged = false;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SyncOverlayPosition();
                    UpdateParallaxOffset();
                    ScanExplorerNavPane();
                    FadeInOverlay();
                }));
                return;
            }

            // Skip location updates during drag
            if (isExplorerBeingDragged)
                return;

            if (eventType == EVENT_OBJECT_LOCATIONCHANGE && hwnd == targetExplorerHwnd)
            {
                long currentTick = Environment.TickCount;
                if (currentTick - lastPositionSyncTick >= POSITION_SYNC_THROTTLE_MS)
                {
                    lastPositionSyncTick = currentTick;
                    SyncOverlayPositionFast();
                }
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

            if ((eventType == EVENT_OBJECT_SELECTION || eventType == EVENT_OBJECT_SELECTIONWITHIN) 
                && hwnd == targetExplorerHwnd)
            {
                Dispatcher.BeginInvoke(new Action(() => ScanExplorerNavPane()), DispatcherPriority.Normal);
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
            StopNavPaneTimers();

            // Clear navpane cache
            lock (_navItemsLock)
            {
                cachedNavItems.Clear();
            }
            Dispatcher.BeginInvoke(() =>
            {
                NavPaneItems.Clear();
            });

            // Reset transparency
            if (cachedHwnd != IntPtr.Zero && isTransparent)
            {
                try
                {
                    long exStyle = GetWindowLongPtr(cachedHwnd, GWL_EXSTYLE).ToInt64();
                    SetWindowLongPtr(
                        cachedHwnd,
                        GWL_EXSTYLE,
                        (IntPtr)(exStyle & ~WS_EX_TRANSPARENT)
                    );
                    isTransparent = false;
                }
                catch { }
            }

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
            if (_isShuttingDown)
                return;

            targetExplorerHwnd = explorerHwnd;
            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;
            cachedHwnd = overlayHwnd;

            // 1. Tempelkan Parent ke Explorer
            SetWindowLongPtr(overlayHwnd, GWL_HWNDPARENT, targetExplorerHwnd);

            if (wallpaperBrush == null)
                _ = LoadDesktopWallpaperAsync();

            // 2. Tampilkan Window & Paksa Z-Order tepat di atas Explorer
            if (Visibility != Visibility.Visible)
                Show();

            // HWND_TOP / SetWindowPos memaksa Overlay berada persis di layer atas Explorer
            SetWindowPos(
                overlayHwnd,
                IntPtr.Zero, // Memaksa posisi Z-Order paling atas di antara sibling window
                0,
                0,
                0,
                0,
                SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
            );

            SyncOverlayPosition();
            UpdateParallaxOffset();
            UpdateExplorerFocusState(GetForegroundWindow());

            // 3. Scan NavPane for both native and non-native mode to get width
            ScanExplorerNavPane();
            StartNavPaneTimers();
        }

        private void StartNavPaneTimers()
        {
            if (_navPaneScanTimer == null)
            {
                _navPaneScanTimer = new System.Windows.Threading.DispatcherTimer();
                _navPaneScanTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _navPaneScanTimer.Tick += OnNavPaneScanTimerTick;
            }
            _navPaneScanTimer?.Start();

            if (_hoverUpdateTimer == null)
            {
                _hoverUpdateTimer = new System.Windows.Threading.DispatcherTimer();
                _hoverUpdateTimer.Interval = TimeSpan.FromMilliseconds(120);
                _hoverUpdateTimer.Tick += OnHoverUpdateTimerTick;
            }
            _hoverUpdateTimer?.Start();
        }

        private void OnNavPaneScanTimerTick(object? sender, EventArgs e)
        {
            if (!lastExplorerFocusVisualState)
            {
                if (Interlocked.Increment(ref _unfocusedScanSkip) < 3)
                    return;
                Interlocked.Exchange(ref _unfocusedScanSkip, 0);
            }
            ScanExplorerNavPane();
        }

        private void OnHoverUpdateTimerTick(object? sender, EventArgs e)
        {
            if (!lastExplorerFocusVisualState)
                return;
            UpdateHoverStateFromAutomation();
        }

        private void StopNavPaneTimers()
        {
            _navPaneScanTimer?.Stop();
            _hoverUpdateTimer?.Stop();
        }

        #endregion

        #region ==================== NATIVE NAVPANE SCANNING ====================

        private async void ScanExplorerNavPane()
        {
            if (_isShuttingDown || Interlocked.CompareExchange(ref isScanning, 1, 0) != 0)
                return;

            IntPtr currentTarget = targetExplorerHwnd;
            if (currentTarget == IntPtr.Zero || !IsWindow(currentTarget))
            {
                Interlocked.Exchange(ref isScanning, 0);
                return;
            }

            AutomationElement? rootElement = null;
            AutomationElement? treeElement = null;

            try
            {
                List<NavPaneItemModel> newItems = await Task.Run(() =>
                {
                    var resultList = new List<NavPaneItemModel>();

                    try
                    {
                        if (
                            DwmGetWindowAttribute(
                                currentTarget,
                                DWMWA_EXTENDED_FRAME_BOUNDS,
                                out RECT explorerRect,
                                Marshal.SizeOf(typeof(RECT))
                            ) != 0
                        )
                        {
                            return resultList;
                        }

                        rootElement = AutomationElement.FromHandle(currentTarget);
                        if (rootElement == null)
                            return resultList;

                        var condition = new PropertyCondition(
                            AutomationElement.ControlTypeProperty,
                            ControlType.Tree
                        );
                        treeElement = rootElement.FindFirst(TreeScope.Descendants, condition);

                        if (treeElement != null)
                        {
                            var treeRect = treeElement.Current.BoundingRectangle;
                            cachedExplorerTop = (int)treeRect.Top;
                            cachedNavPaneWidth = (int)(treeRect.Right - treeRect.Left);

                            var itemCondition = new PropertyCondition(
                                AutomationElement.ControlTypeProperty,
                                ControlType.TreeItem
                            );
                            var items = treeElement.FindAll(TreeScope.Descendants, itemCondition);

                            if (items != null)
                            {
                                var seenItems = new Dictionary<string, double>();

                                foreach (AutomationElement item in items)
                                {
                                    try
                                    {
                                        string cleanName = item.Current.Name ?? string.Empty;

                                        if (cleanName.StartsWith("Start of Quick Access - ", StringComparison.Ordinal))
                                            cleanName = cleanName.Substring(25);
                                        else if (cleanName.StartsWith("End of Quick Access - ", StringComparison.Ordinal))
                                            cleanName = cleanName.Substring(22);
                                        else if (cleanName.StartsWith("Start of ", StringComparison.Ordinal))
                                            cleanName = cleanName.Substring(9);
                                        else if (cleanName.StartsWith("End of ", StringComparison.Ordinal))
                                            cleanName = cleanName.Substring(7);

                                        int pinnedIdx = cleanName.IndexOf("(pinned)", StringComparison.Ordinal);
                                        cleanName = (
                                            pinnedIdx >= 0
                                                ? cleanName.Remove(pinnedIdx, 8)
                                                : cleanName
                                        ).Trim();

                                        var itemRect = item.Current.BoundingRectangle;
                                        double height = itemRect.Bottom - itemRect.Top;
                                        double relativeY = itemRect.Top - explorerRect.Top;

                                        if (relativeY < 50)
                                            continue;

                                        if (!string.IsNullOrWhiteSpace(cleanName) && height > 0)
                                        {
                                            bool skipDuplicate = false;
                                            if (
                                                seenItems.TryGetValue(
                                                    cleanName,
                                                    out double existingY
                                                )
                                            )
                                            {
                                                if (Math.Abs(relativeY - existingY) < 30)
                                                    skipDuplicate = true;
                                            }
                                            if (skipDuplicate)
                                                continue;

                                            seenItems[cleanName] = relativeY;

                                            bool isSelected = false;
                                            try
                                            {
                                                if (
                                                    item.GetCurrentPattern(
                                                        SelectionItemPattern.Pattern
                                                    )
                                                    is SelectionItemPattern selPattern
                                                )
                                                {
                                                    isSelected = selPattern.Current.IsSelected;
                                                }
                                            }
                                            catch { }

                                            resultList.Add(
                                                new NavPaneItemModel
                                                {
                                                    Name = cleanName,
                                                    YOffset = relativeY,
                                                    XIndent = 0,
                                                    Height = height,
                                                    IsSelected = isSelected,
                                                    IsHovered = false,
                                                    IsPressed = false,
                                                }
                                            );
                                        }
                                    }
                                    catch (Exception itemEx)
                                    {
                                        Debug.WriteLine(
                                            $"ScanExplorerNavPane item error: {itemEx.Message}"
                                        );
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ScanExplorerNavPane background error: {ex.Message}");
                    }

                    return resultList;
                });

                if (!_isShuttingDown && newItems != null)
                {
                    ApplyNavPaneItemsUpdate(newItems);
                    
                    if (Interlocked.Increment(ref gcCounter) >= GC_COLLECT_INTERVAL)
                    {
                        Interlocked.Exchange(ref gcCounter, 0);
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                    }
                    
                    if (cachedNavPaneWidth > 0)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            if (this.Content is FrameworkElement root)
                            {
                                var sidebarCol = FindElementByName<ColumnDefinition>(root, "SidebarColumn");
                                if (sidebarCol != null)
                                {
                                    sidebarCol.Width = new GridLength(cachedNavPaneWidth + 10);
                                }
                            }
                        }, DispatcherPriority.Loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScanExplorerNavPane error: {ex.Message}");
            }
            finally
            {
                ReleaseComObject(treeElement);
                ReleaseComObject(rootElement);
                Interlocked.Exchange(ref isScanning, 0);
            }
        }

        private int gcCounter = 0;
        private const int GC_COLLECT_INTERVAL = 50;
        private int _unfocusedScanSkip = 0;

        private void ReleaseComObject(object? obj)
        {
            if (obj == null) return;
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
            }
            catch { }
        }

        /// <summary>
        /// Helper untuk memperbarui ObservableCollection tanpa menghancurkan visual tree jika tidak ada perubahan struktur
        /// </summary>
        private void ApplyNavPaneItemsUpdate(List<NavPaneItemModel> newItems)
        {
            lock (_navItemsLock)
            {
                bool hasChanges = cachedNavItems.Count != newItems.Count;
                if (!hasChanges)
                {
                    for (int i = 0; i < newItems.Count; i++)
                    {
                        if (
                            cachedNavItems[i].Name != newItems[i].Name
                            || Math.Abs(cachedNavItems[i].YOffset - newItems[i].YOffset) > 2
                            || cachedNavItems[i].IsSelected != newItems[i].IsSelected
                        )
                        {
                            hasChanges = true;
                            break;
                        }
                    }
                }

                if (hasChanges)
                {
                    for (int i = 0; i < newItems.Count; i++)
                    {
                        if (i < cachedNavItems.Count)
                        {
                            cachedNavItems[i].Name = newItems[i].Name;
                            cachedNavItems[i].YOffset = newItems[i].YOffset;
                            cachedNavItems[i].Height = newItems[i].Height;
                            cachedNavItems[i].IsSelected = newItems[i].IsSelected;
                            cachedNavItems[i].IsHovered = false;
                            cachedNavItems[i].IsPressed = false;
                        }
                        else
                        {
                            cachedNavItems.Add(newItems[i]);
                        }
                    }
                    
                    while (cachedNavItems.Count > newItems.Count)
                    {
                        cachedNavItems.RemoveAt(cachedNavItems.Count - 1);
                    }

                    NavPaneItems.Clear();
                    foreach (var item in cachedNavItems)
                    {
                        NavPaneItems.Add(item);
                    }
                }
            }
        }

        private POINT _lastMousePos = new POINT { X = -1, Y = -1 };
        private bool _lastMouseDownState = false;

        private void UpdateHoverStateFromAutomation()
        {
            try
            {
                // 1. Guard Clauses Cepat (Early Exit)
                if (_isShuttingDown || appSettings.NativeNavPane != 1)
                    return;

                if (targetExplorerHwnd == IntPtr.Zero || !IsWindow(targetExplorerHwnd))
                    return;

                if (this.Visibility != Visibility.Visible)
                    return;

                IntPtr foregroundHwnd = GetForegroundWindow();

                // Ambil data item dengan lock minimal
                List<NavPaneItemModel> localItems;
                lock (_navItemsLock)
                {
                    if (cachedNavItems.Count == 0)
                        return;
                    localItems = cachedNavItems;
                }

                // 2. Cek posisi Mouse & Klik
                GetCursorPos(out POINT mousePt);
                bool isMouseDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

                // Jika mouse dan tombol klik tidak berubah posisi sama sekali, lewati kalkulasi Win32
                if (
                    mousePt.X == _lastMousePos.X
                    && mousePt.Y == _lastMousePos.Y
                    && isMouseDown == _lastMouseDownState
                )
                {
                    return;
                }

                _lastMousePos = mousePt;
                _lastMouseDownState = isMouseDown;

                // 3. Kalkulasi Koordinat
                if (
                    DwmGetWindowAttribute(
                        targetExplorerHwnd,
                        DWMWA_EXTENDED_FRAME_BOUNDS,
                        out RECT explorerRect,
                        Marshal.SizeOf(typeof(RECT))
                    ) != 0
                )
                {
                    return;
                }

                int navPaneWidth =
                    cachedNavPaneWidth > 0 ? cachedNavPaneWidth : (int)SIDEBAR_DEFAULT_WIDTH;
                int sidebarLeft = explorerRect.Left;
                int sidebarRight = explorerRect.Left + navPaneWidth;
                int sidebarTop = explorerRect.Top;
                int sidebarBottom = explorerRect.Bottom;

                // 3.5. Drag detection is handled by EVENT_SYSTEM_MOVESIZESTART/END hooks
                // No title bar detection in timer to avoid interfering with button clicks

                // 4. Cari item yang sedang di-hover
                int hoveredIndex = -1;
                bool isMouseInSidebar = mousePt.X >= sidebarLeft && mousePt.X <= sidebarRight 
                    && mousePt.Y >= sidebarTop && mousePt.Y <= sidebarBottom;

                if (isMouseInSidebar)
                {
                    for (int i = 0; i < localItems.Count; i++)
                    {
                        var item = localItems[i];
                        int itemTop = sidebarTop + (int)item.YOffset;
                        int itemBottom = itemTop + (int)item.Height;

                        if (mousePt.Y >= itemTop && mousePt.Y < itemBottom)
                        {
                            hoveredIndex = i;
                            break; // Ditemukan, langsung keluar loop
                        }
                    }
                }

                bool isMouseOverNavItem = hoveredIndex != -1;

                // 6. Update Style Transparan Jendela (Hanya panggil SetWindowLongPtr jika status berubah)
                if (cachedHwnd != IntPtr.Zero)
                {
                    long exStyle = GetWindowLongPtr(cachedHwnd, GWL_EXSTYLE).ToInt64();
                    bool currentlyTransparent = (exStyle & WS_EX_TRANSPARENT) != 0;

                    if (isMouseOverNavItem && !currentlyTransparent)
                    {
                        SetWindowLongPtr(
                            cachedHwnd,
                            GWL_EXSTYLE,
                            (IntPtr)(exStyle | WS_EX_TRANSPARENT)
                        );
                        isTransparent = true;
                    }
                    else if (!isMouseOverNavItem && currentlyTransparent)
                    {
                        SetWindowLongPtr(
                            cachedHwnd,
                            GWL_EXSTYLE,
                            (IntPtr)(exStyle & ~WS_EX_TRANSPARENT)
                        );
                        isTransparent = false;
                    }
                }

                lastIsExplorerFocused = (GetForegroundWindow() == targetExplorerHwnd);
                if (!lastIsExplorerFocused)
                    return;

                // 7. In-Place Update Properti (Tanpa Clear / Re-create List)
                for (int i = 0; i < localItems.Count; i++)
                {
                    var item = localItems[i];
                    bool shouldHover = (hoveredIndex == i) && !item.IsSelected;
                    bool shouldPress = shouldHover && isMouseDown;

                    // Karena mengimplementasikan INotifyPropertyChanged, UI akan otomatis update
                    item.IsHovered = shouldHover;
                    item.IsPressed = shouldPress;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateHoverStateFromAutomation error: {ex.Message}");
            }
        }

        #endregion
    }
}
