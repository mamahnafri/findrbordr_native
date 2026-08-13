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

    public class NavPaneItemModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private double _yOffset;
        private double _xIndent;
        private double _height;
        private bool _isSelected;
        private bool _isHovered;
        private bool _isPressed;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public double YOffset
        {
            get => _yOffset;
            set
            {
                if (Math.Abs(_yOffset - value) > 0.1)
                {
                    _yOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        public double XIndent
        {
            get => _xIndent;
            set
            {
                if (Math.Abs(_xIndent - value) > 0.1)
                {
                    _xIndent = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                if (Math.Abs(_height - value) > 0.1)
                {
                    _height = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Model utama konfigurasi aplikasi yang memuat Dynamic Resource XAML & Shadow Settings
    /// </summary>
    public class AppSettings
    {
        public string ThemeXamlPath { get; set; } = "Themes/Default.xaml";

        // --- Sidebar Visibility ---
        public int SidebarVisible { get; set; } = 1;

        // --- Native NavPane Mode (0 = custom shortcuts, 1 = native explorer navpane) ---
        public int NativeNavPane { get; set; } = 1;

        // --- Outer Cosmetic Border Visibility ---
        public int OuterCosmeticBorderVisible { get; set; } = 1;

        // --- Toolbar Position Offset ---
        public double ToolbarPosX { get; set; } = 0;
        public double ToolbarPosY { get; set; } = 3;

        // --- Dynamic Resource Brushes ---
        public double SidebarWidth { get; set; } = 185;
        public string CapsuleBackgroundBrush { get; set; } = "#FFFFFF";
        public string MainTextBrush { get; set; } = "#2C3E50";
        public string OuterFrameBrush { get; set; } = "#FFFFFF";

        // --- Drop Shadows & Borders ---
        public BrushSettings Layer3BorderBackground { get; set; } = new BrushSettings();
        public ShadowSettings LeftSidebarGridShadow { get; set; } = new ShadowSettings();

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
            DefaultIgnoreCondition = System
                .Text
                .Json
                .Serialization
                .JsonIgnoreCondition
                .WhenWritingNull,
        };

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(FilePath))
            {
                var defaultSettings = new AppSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (loadedSettings == null)
                    return new AppSettings();

                var defaultSettings = new AppSettings();
                bool needsSave = false;

                using var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty(nameof(AppSettings.ThemeXamlPath), out _))
                {
                    loadedSettings.ThemeXamlPath = defaultSettings.ThemeXamlPath;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.SidebarVisible), out _))
                {
                    loadedSettings.SidebarVisible = defaultSettings.SidebarVisible;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.OuterCosmeticBorderVisible), out _))
                {
                    loadedSettings.OuterCosmeticBorderVisible =
                        defaultSettings.OuterCosmeticBorderVisible;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.ToolbarPosX), out _))
                {
                    loadedSettings.ToolbarPosX = defaultSettings.ToolbarPosX;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.ToolbarPosY), out _))
                {
                    loadedSettings.ToolbarPosY = defaultSettings.ToolbarPosY;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.SidebarWidth), out _))
                {
                    loadedSettings.SidebarWidth = defaultSettings.SidebarWidth;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.CapsuleBackgroundBrush), out _))
                {
                    loadedSettings.CapsuleBackgroundBrush = defaultSettings.CapsuleBackgroundBrush;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.MainTextBrush), out _))
                {
                    loadedSettings.MainTextBrush = defaultSettings.MainTextBrush;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.OuterFrameBrush), out _))
                {
                    loadedSettings.OuterFrameBrush = defaultSettings.OuterFrameBrush;
                    needsSave = true;
                }

                if (loadedSettings.Layer3BorderBackground == null)
                {
                    loadedSettings.Layer3BorderBackground = defaultSettings.Layer3BorderBackground;
                    needsSave = true;
                }

                if (loadedSettings.LeftSidebarGridShadow == null)
                {
                    loadedSettings.LeftSidebarGridShadow = defaultSettings.LeftSidebarGridShadow;
                    needsSave = true;
                }

                if (loadedSettings.CustomShortcuts == null)
                {
                    loadedSettings.CustomShortcuts = defaultSettings.CustomShortcuts;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.NativeNavPane), out _))
                {
                    loadedSettings.NativeNavPane = defaultSettings.NativeNavPane;
                    needsSave = true;
                }

                if (needsSave)
                    SaveSettings(loadedSettings);

                return loadedSettings;
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
        #region ==================== CONSTRUCTOR & INITIALIZATION ====================

        public MainWindow()
        {
            // Atur properti Window di C# karena tidak bisa di XAML eksternal
            this.Title = "Finder Overlay Native";
            this.Width = 1200;
            this.Height = 800;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.ResizeMode = ResizeMode.NoResize;
            this.ShowInTaskbar = false;
            this.WindowStyle = WindowStyle.None;
            this.AllowDrop = true;

            // 1. ContextMenu Global Handler (Sudah ada di kode Anda)
            EventManager.RegisterClassHandler(
                typeof(ContextMenu),
                MenuItem.ClickEvent,
                new RoutedEventHandler(ContextMenuItem_Click)
            );

            // 2. TAMBAHKAN INI: Button Global Handler
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(GlobalButton_Click)
            );

            InitSettingsAndShortcuts();

            Type? wshellType = Type.GetTypeFromProgID("WScript.Shell");
            if (wshellType != null)
                wshellInstance = Activator.CreateInstance(wshellType);

            this.Loaded += MainWindow_Loaded;
            this.LocationChanged += OnLocationChanged;

            UserProfileName = Environment.UserName;
            this.DataContext = this;

            this.PreviewMouseDown += OnPreviewMouseDown;

            this.PreviewDragOver += Window_PreviewDragOver;
            this.PreviewDrop += Window_PreviewDrop;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 1. Muat UI dari XAML Eksternal setelah HWND terbentuk
            LoadExternalXamlUI();

            // 2. Set Win32 Styles agar menjadi ToolWindow tanpa fokus terpisah
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            long exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(
                hwnd,
                GWL_EXSTYLE,
                (IntPtr)(exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED)
            );

            hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource?.AddHook(WndProc);

            // 3. Pasang Hook ke File Explorer
            Dispatcher.BeginInvoke(
                new Action(() => RegisterWinEventHooks()),
                DispatcherPriority.Render
            );
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDesktopWallpaperAsync();
            ApplyAppSettingsToUI();
        }

        #endregion

        #region ==================== CLEANUP & DISPOSAL ====================

        private HwndSource? hwndSource;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            this.Loaded -= MainWindow_Loaded;
            this.LocationChanged -= OnLocationChanged;
            this.PreviewMouseDown -= OnPreviewMouseDown;
            this.PreviewDragOver -= Window_PreviewDragOver;
            this.PreviewDrop -= Window_PreviewDrop;

            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource.Dispose();
                hwndSource = null;
            }

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
            if (moveSizeStartHook != IntPtr.Zero)
            {
                UnhookWinEvent(moveSizeStartHook);
                moveSizeStartHook = IntPtr.Zero;
            }
            if (moveSizeEndHook != IntPtr.Zero)
            {
                UnhookWinEvent(moveSizeEndHook);
                moveSizeEndHook = IntPtr.Zero;
            }
            if (selectionHook != IntPtr.Zero)
            {
                UnhookWinEvent(selectionHook);
                selectionHook = IntPtr.Zero;
            }

            if (wshellInstance != null)
            {
                Marshal.ReleaseComObject(wshellInstance);
                wshellInstance = null;
            }

            StopNavPaneTimers();
            _hoverUpdateTimer = null;
            _navPaneScanTimer = null;

            _altTabRetryCts?.Cancel();
            _altTabRetryCts?.Dispose();
            _altTabRetryCts = null;

            _isShuttingDown = true;
        }

        private void OnLocationChanged(object? sender, EventArgs e) => UpdateParallaxOffset();

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only force focus to Explorer when not in native navpane mode
            if (appSettings.NativeNavPane != 1)
            {
                if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                {
                    SetForegroundWindow(targetExplorerHwnd);
                }
            }
        }

        private void LoadExternalXamlUI()
        {
            try
            {
                string xamlPath = appSettings.ThemeXamlPath;

                if (!Path.IsPathRooted(xamlPath))
                {
                    xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, xamlPath);
                }

                if (File.Exists(xamlPath))
                {
                    using (FileStream fs = new FileStream(xamlPath, FileMode.Open, FileAccess.Read))
                    {
                        FrameworkElement rootContent = (FrameworkElement)XamlReader.Load(fs);
                        this.Content = rootContent;

                        if (
                            FindElementByName<Border>(rootContent, "DropZoneBorder")
                            is Border dropZone
                        )
                        {
                            dropZone.AllowDrop = true;
                            dropZone.DragOver += DropZone_DragOver;
                            dropZone.Drop += DropZone_Drop;
                            dropZone.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                            Debug.WriteLine("[DEBUG] DropZoneBorder found, events attached!");
                        }
                        else
                        {
                            Debug.WriteLine("[DEBUG] ERROR: DropZoneBorder NOT FOUND!");
                        }

                        rootContent.DataContext = this;

                        if (
                            FindElementByName<ItemsControl>(rootContent, "CustomShortcutsList")
                            is ItemsControl listControl
                        )
                        {
                            listControl.ItemsSource = customShortcuts;
                        }

                        if (
                            FindElementByName<ItemsControl>(rootContent, "NativeNavPaneCanvas")
                            is ItemsControl navPaneControl
                        )
                        {
                            navPaneControl.ItemsSource = NavPaneItems;
                        }
                    }

                    // Tunggu Visual Tree ter-render sempurna sebelum menyambungkan event & attach ke Explorer
                    Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            ApplyAppSettingsToUI();

                            if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                            {
                                AttachToExplorer(targetExplorerHwnd);
                            }
                            else
                            {
                                TryFindAndAttachExplorer();
                            }
                        }),
                        DispatcherPriority.Loaded
                    );
                }
                else
                {
                    Debug.WriteLine($"File XAML tema tidak ditemukan: {xamlPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat XAML Tema:\n{ex.Message}", "Error Load Theme");
            }
        }

        /// <summary>
        /// Helper khusus untuk mencari elemen secara rekursif berdasarkan Name di Visual Tree / Logical Tree
        /// (Mendukung FrameworkElement maupun ColumnDefinition/RowDefinition)
        /// </summary>
        internal T? FindElementByName<T>(DependencyObject? parent, string name)
            where T : DependencyObject
        {
            if (parent == null)
                return null;

            // 1. Cek jika parent itu sendiri adalah Grid dan kita mencari ColumnDefinition
            if (parent is Grid grid && typeof(T) == typeof(ColumnDefinition))
            {
                foreach (var col in grid.ColumnDefinitions)
                {
                    if (col.Name == name)
                        return col as T;
                }
            }

            // 2. Telusuri anak visual (Visual Tree)
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe && fe.Name == name && child is T match)
                    return match;

                var result = FindElementByName<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Hubungkan kembali event-event UI menggunakan penelusuran Visual Tree
        /// </summary>
        private void GlobalButton_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn)
            {
                // 1. Handle Mac Dots (Close, Minimize, Maximize)
                if (btn.Name == "DotClose")
                {
                    BtnClose_Click(btn, e);
                    return;
                }
                if (btn.Name == "DotMinimize")
                {
                    BtnMinimize_Click(btn, e);
                    return;
                }
                if (btn.Name == "DotMaximize")
                {
                    BtnMaximize_Click(btn, e);
                    return;
                }

                // 2. Handle Tombol Navigasi Sidebar (Applications, Desktop, Documents, dll)
                if (btn.Name.StartsWith("BtnNav"))
                {
                    Nav_Click(btn, e);
                    return;
                }

                // 3. Handle Folder Options
                if (btn.Name == "BtnFolderOptions")
                {
                    BtnFolderOptions_Click(btn, e);
                    return;
                }

                // 4. Handle Navigation Shortcut (path folder)
                if (btn.Tag is string path && !string.IsNullOrWhiteSpace(path))
                {
                    bool isNavigationPath =
                        System.IO.Directory.Exists(path)
                        || path.StartsWith("shell:")
                        || Enum.TryParse(path, out Environment.SpecialFolder _);

                    if (isNavigationPath)
                    {
                        Nav_Click(btn, e);
                        return;
                    }

                    // 5. Handle File Shortcut (execute file)
                    if (System.IO.File.Exists(path))
                    {
                        try
                        {
                            Process.Start(
                                new ProcessStartInfo { FileName = path, UseShellExecute = true }
                            );
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to execute shortcut: {ex.Message}");
                        }
                        return;
                    }

                    // 6. Handle SendKeys Shortcut
                    SendKeysToExplorer(path);
                    return;
                }
            }
        }

        private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // e.OriginalSource adalah MenuItem yang diklik (dari routed event)
            if (e.OriginalSource is MenuItem menuItem)
            {
                string header = menuItem.Header?.ToString() ?? "";
                string tag = menuItem.Tag?.ToString() ?? "";

                Debug.WriteLine($"[DEBUG] ContextMenu Item Clicked: '{header}' (Tag: '{tag}')");

                switch (header)
                {
                    case "Settings":
                        BtnSettings_Click(menuItem, e);
                        break;
                    case "Apply Settings":
                        BtnReloadJson_Click(menuItem, e);
                        break;
                    case "Relaunch App":
                        BtnRelaunch_Click(menuItem, e);
                        break;
                    case "Exit App":
                        BtnExit_Click(menuItem, e);
                        break;
                    case "Delete":
                        BtnDeleteShortcut_Click(menuItem, e);
                        break;
                }

                if (
                    !string.IsNullOrEmpty(tag)
                    && (tag == "n" || tag == "d" || tag == "t" || tag == "asc" || tag == "desc")
                )
                {
                    SortOption_Click(menuItem, e);
                }
            }
        }

        #endregion

        #region ==================== DYNAMIC XAML ELEMENT GETTERS ====================

        public Grid? ParallaxCanvas =>
            FindElementByName<Grid>(this.Content as FrameworkElement, "ParallaxCanvas");

        public TextBlock? TxtTitle =>
            FindElementByName<TextBlock>(this.Content as FrameworkElement, "TxtTitle");

        public StackPanel? SidebarContainer =>
            FindElementByName<StackPanel>(this.Content as FrameworkElement, "SidebarContainer");

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj)
            where T : DependencyObject
        {
            if (depObj == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T t)
                {
                    yield return t;
                }

                if (child != null)
                {
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Window_PreviewDrop triggered!");
            DropZone_Drop(sender, e);
        }

        #endregion
    }
}
