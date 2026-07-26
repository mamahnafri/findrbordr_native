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
        // --- Sidebar Visibility ---
        public int SidebarVisible { get; set; } = 1;

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

                if (!root.TryGetProperty(nameof(AppSettings.SidebarVisible), out _))
                {
                    loadedSettings.SidebarVisible = defaultSettings.SidebarVisible;
                    needsSave = true;
                }

                if (!root.TryGetProperty(nameof(AppSettings.OuterCosmeticBorderVisible), out _))
                {
                    loadedSettings.OuterCosmeticBorderVisible = defaultSettings.OuterCosmeticBorderVisible;
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
            InitSettingsAndShortcuts();
            InitializeComponent();

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
                (IntPtr)(exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED)
            );

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);

            Dispatcher.BeginInvoke(
                new Action(() => RegisterWinEventHooks()),
                DispatcherPriority.Render
            );
        }

        #endregion

        #region ==================== CLEANUP & DISPOSAL ====================

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (wshellInstance != null)
            {
                Marshal.ReleaseComObject(wshellInstance);
                wshellInstance = null;
            }
        }
        #endregion
    }
}
