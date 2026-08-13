namespace findrbordr_native
{
    public partial class MainWindow
    {
        #region ==================== WINDOW CONTROL & COMMANDS ====================

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;

        public void SimulateSoftClickInFolderView(IntPtr explorerHwnd)
        {
            IntPtr shellTabWnd = FindWindowEx(
                explorerHwnd,
                IntPtr.Zero,
                "ShellTabWindowClass",
                null
            );
            IntPtr targetContainer = shellTabWnd != IntPtr.Zero ? shellTabWnd : explorerHwnd;
            IntPtr defViewHwnd = FindWindowEx(
                targetContainer,
                IntPtr.Zero,
                "SHELLDLL_DefView",
                null
            );

            if (defViewHwnd != IntPtr.Zero)
            {
                // Kirim klik ke koordinat internal (X: 10, Y: 10) di dalam area view folder
                IntPtr lParam = (IntPtr)((10 & 0xFFFF) | ((10 & 0xFFFF) << 16));

                PostMessage(defViewHwnd, WM_LBUTTONDOWN, (IntPtr)0x0001, lParam);
                PostMessage(defViewHwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
            }
        }

        //cek sebelum eksekusi tombol enter
        private bool IsAddressBarFocused()
        {
            try
            {
                // Ambil elemen UI yang saat ini sedang aktif memegang FOKUS di seluruh Windows
                AutomationElement focusedElement = AutomationElement.FocusedElement;

                // Cek apakah elemen tersebut bertipe Edit (Address Bar)
                if (
                    focusedElement != null
                    && focusedElement.Current.ControlType == ControlType.Edit
                )
                {
                    return true;
                }
            }
            catch { }

            return false; // Fokus berpindah ke tempat lain (misal ke list file)!
        }

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

        private void SortOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string optionKey)
            {
                // Unstuck dulu fokus Explorer
                SimulateSoftClickInFolderView(targetExplorerHwnd);

                // Eksekusi shortcut Context Menu Explorer di background:
                // Shift+F10 (Context Menu) -> 'o' (Sort by) -> key pilihan ('n', 'd', 't', dll.)
                if (optionKey == "asc" || optionKey == "desc")
                {
                    // Untuk Ascending / Descending
                    SendKeysToExplorer("+{F10}o");
                }
                else
                {
                    // Untuk Name, Date, Type
                    SendKeysToExplorer($"+{{F10}}o{optionKey}");
                }
            }
        }

        private bool isNavigating = false;

        private async void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (isNavigating)
                return;

            // Ambil Button dari OriginalSource jika menggunakan AddHandler
            Button? btn = sender as Button ?? e.OriginalSource as Button;

            if (
                btn != null
                && btn.Tag is string pathTarget
                && !string.IsNullOrWhiteSpace(pathTarget)
                && targetExplorerHwnd != IntPtr.Zero
            )
            {
                isNavigating = true;

                try
                {
                    string targetPath = pathTarget;

                    if (Enum.TryParse(pathTarget, out Environment.SpecialFolder folder))
                    {
                        targetPath = Environment.GetFolderPath(folder);
                    }

                    // 1. DEKLARASIKAN ESCAPEDPATH DI SINI SEBELUM DIPAKAI
                    string escapedPath = EscapeSendKeysString(targetPath);

                    // 2. Unstuck & Navigasi
                    SimulateSoftClickInFolderView(targetExplorerHwnd);

                    await SendKeysToExplorerAsync("%d", 60);
                    await Task.Delay(50);
                    // Cek apakah address bar benar-benar mendapat fokus
                    if (IsAddressBarFocused())
                    {
                        await SendKeysToExplorerAsync(escapedPath, 60);
                        await SendKeysToExplorerAsync("{ENTER}", 60);
                        await Task.Delay(400);
                    }
                    else
                    {
                        // Opsional: Handle jika fokus gagal/berpindah
                        // Misalnya log error, retry, atau batalkan eksekusi
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Nav_Click error: " + ex.Message);
                }
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



        // Single Handler for Navigation & View Shortcuts (Set Tag in XAML, e.g. Tag="%{LEFT}")
    private void BtnShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string keys) SendKeysToExplorer(keys);
    }

    private void BtnFolderOptions_Click(object sender, RoutedEventArgs e) { try { Process.Start("control.exe", "folders"); } catch { } }
    private void BtnClose_Click(object sender, RoutedEventArgs e) { if (targetExplorerHwnd != IntPtr.Zero) SendMessage(targetExplorerHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); }
    private void BtnMinimize_Click(object sender, RoutedEventArgs e) { if (targetExplorerHwnd != IntPtr.Zero) SendMessage(targetExplorerHwnd, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero); }
    private void BtnMaximize_Click(object sender, RoutedEventArgs e) { if (targetExplorerHwnd != IntPtr.Zero) SendMessage(targetExplorerHwnd, WM_SYSCOMMAND, IsZoomed(targetExplorerHwnd) ? (IntPtr)SC_RESTORE : (IntPtr)SC_MAXIMIZE, IntPtr.Zero); }
    private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();



        #endregion
        #region ==================== APP SETTINGS & SHORTCUTS LOGIC ====================

        private void InitSettingsAndShortcuts()
        {
            try
            {
                appSettings = SettingsStorage.LoadSettings();
                customShortcuts =
                    appSettings.CustomShortcuts ?? new ObservableCollection<ShortcutItem>();

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (
                            this.Content is FrameworkElement root
                            && FindElementByName<ItemsControl>(root, "CustomShortcutsList")
                                is ItemsControl listControl
                        )
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
                ApplySidebarVisibility();
                ApplyOuterCosmeticBorderVisibility();
                ApplyToolbarPosition();

                // Content langsung adalah root dari XAML theme
                if (this.Content is not FrameworkElement root)
                    return;

                // 1. Set Lebar Column Sidebar - use cachedNavPaneWidth from Explorer navpane
                var sidebarCol = FindElementByName<ColumnDefinition>(root, "SidebarColumn");
                if (sidebarCol != null)
                {
                    double actualWidth = appSettings.SidebarWidth;
                    if (cachedNavPaneWidth > 0)
                    {
                        actualWidth = cachedNavPaneWidth + 10;
                    }
                    if (actualWidth > 0)
                    {
                        sidebarCol.Width = new GridLength(actualWidth);
                    }
                }

                // 2. Set Warna Capsule Background Brush
                if (!string.IsNullOrEmpty(appSettings.CapsuleBackgroundBrush))
                {
                    var capsuleColor = (Color)
                        ColorConverter.ConvertFromString(appSettings.CapsuleBackgroundBrush);
                    root.Resources["CapsuleBackgroundBrush"] = new SolidColorBrush(capsuleColor);
                }

                // 3. Set Warna Main Text
                if (!string.IsNullOrEmpty(appSettings.MainTextBrush))
                {
                    var textColor = (Color)
                        ColorConverter.ConvertFromString(appSettings.MainTextBrush);
                    root.Resources["MainTextBrush"] = new SolidColorBrush(textColor);
                }

                // 4. Set Warna Outer Frame
                if (!string.IsNullOrEmpty(appSettings.OuterFrameBrush))
                {
                    var outerColor = (Color)
                        ColorConverter.ConvertFromString(appSettings.OuterFrameBrush);
                    root.Resources["OuterFrameBrush"] = new SolidColorBrush(outerColor);
                }

                // 5. Set DropShadow Sidebar
                if (
                    FindElementByName<Border>(root, "LeftSidebarShadowBorder")
                        is Border shadowBorder
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

                // 6. Set Background Layer3 Border
                if (
                    FindElementByName<Border>(root, "Layer3Border") is Border layer3Border
                    && appSettings.Layer3BorderBackground != null
                )
                {
                    var color = (Color)
                        ColorConverter.ConvertFromString(
                            appSettings.Layer3BorderBackground.ColorHex
                        );
                    layer3Border.Background = new SolidColorBrush(color);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal menerapkan UI settings: " + ex.Message);
            }
        }

        private void ApplyOuterCosmeticBorderVisibility()
        {
            bool showBorder = appSettings.OuterCosmeticBorderVisible != 0;

            if (
                this.Content is FrameworkElement root
                && FindElementByName<Border>(root, "OuterCosmeticFrameBorder") is Border outerBorder
            )
            {
                outerBorder.Visibility = showBorder ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApplyToolbarPosition()
        {
            if (
                this.Content is FrameworkElement root
                && FindElementByName<Grid>(root, "ToolbarGrid") is Grid toolbarGrid
            )
            {
                toolbarGrid.RenderTransform = new TranslateTransform(
                    appSettings.ToolbarPosX,
                    appSettings.ToolbarPosY
                );
            }
        }

        private void ApplySidebarVisibility()
        {
            bool showSidebar = appSettings.SidebarVisible != 0;
            bool useNativeNavPane = appSettings.NativeNavPane == 1;
            if (this.Content is not FrameworkElement root)
                return;

            // 1. Ubah Lebar Kolom - use cachedNavPaneWidth from Explorer navpane
            var sidebarCol = FindElementByName<ColumnDefinition>(root, "SidebarColumn");
            if (sidebarCol != null)
            {
                double actualWidth = appSettings.SidebarWidth;
                if (cachedNavPaneWidth > 0)
                {
                    actualWidth = cachedNavPaneWidth + 10;
                }
                sidebarCol.Width = showSidebar
                    ? new GridLength(actualWidth)
                    : new GridLength(50);
            }

            // 2. Sembunyikan Layer Sidebar saat ditutup
            if (FindElementByName<Border>(root, "LeftSidebarShadowBorder") is Border shadowBorder)
            {
                shadowBorder.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;
                shadowBorder.IsHitTestVisible = showSidebar && !useNativeNavPane;
            }

            if (FindElementByName<Border>(root, "SidebarGlassBorder") is Border glassBorder)
                glassBorder.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;

            if (FindElementByName<Border>(root, "Layer3Border") is Border layer3Border)
            {
                layer3Border.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;
                layer3Border.IsHitTestVisible = showSidebar && !useNativeNavPane;
            }

            // 3. Toggle Custom vs Native NavPane
            var customScroller = FindElementByName<ScrollViewer>(root, "CustomSidebarScroller");
            var nativeCanvas = FindElementByName<Canvas>(root, "NativeNavPaneCanvas");

            if (customScroller != null && nativeCanvas != null)
            {
                if (useNativeNavPane)
                {
                    customScroller.Visibility = Visibility.Collapsed;
                    nativeCanvas.Visibility = Visibility.Visible;
                }
                else
                {
                    customScroller.Visibility = Visibility.Visible;
                    nativeCanvas.Visibility = Visibility.Collapsed;
                }
            }

            // 4. Scan NavPane for both modes to get width
            if (targetExplorerHwnd != IntPtr.Zero)
            {
                ScanExplorerNavPane();
                StartNavPaneTimers();
            }
            else
            {
                StopNavPaneTimers();
            }
        }

        private void SaveAllSettings()
        {
            appSettings.CustomShortcuts = customShortcuts;
            SettingsStorage.SaveSettings(appSettings);
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("[DEBUG] DropZone_DragOver triggered!");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine("[DEBUG] DropZone_Drop triggered!");

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                Debug.WriteLine($"[DEBUG] Files dropped: {files?.Length ?? 0}");
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
                        Debug.WriteLine($"[DEBUG] Saving {customShortcuts.Count} shortcuts to JSON...");
                        SaveAllSettings();
                        Debug.WriteLine("[DEBUG] Settings saved!");
                        if (
                            this.Content is FrameworkElement root
                            && FindElementByName<ItemsControl>(root, "CustomShortcutsList")
                                is ItemsControl listControl
                        )
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
            // Ambil MenuItem dari OriginalSource untuk global handler
            MenuItem? menuItem = sender as MenuItem ?? e.OriginalSource as MenuItem;

            if (menuItem != null && menuItem.DataContext is ShortcutItem itemToRemove)
            {
                customShortcuts.Remove(itemToRemove);
                SaveAllSettings();

                if (
                    this.Content is FrameworkElement root
                    && FindElementByName<ItemsControl>(root, "CustomShortcutsList")
                        is ItemsControl listControl
                )
                {
                    listControl.ItemsSource = null;
                    listControl.ItemsSource = customShortcuts;
                }
            }
        }

        private void Sidebar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Jangan blok context menu dari child elements
            // Biarkan context menu dari Grid utama maupun child elements sama-sama bisa terbuka
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "app_settings.json"
                );

                if (!System.IO.File.Exists(settingsPath))
                {
                    SaveAllSettings();
                }

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

                ApplyAppSettingsToUI();
                RefreshCustomShortcutsList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal reload JSON/XAML settings: " + ex.Message);
            }
        }

        private void RefreshCustomShortcutsList()
        {
            if (this.Content is not FrameworkElement root)
                return;

            if (FindElementByName<ItemsControl>(root, "CustomShortcutsList") is ItemsControl listControl)
            {
                listControl.ItemsSource = null;
                listControl.ItemsSource = customShortcuts;
            }
        }

        private async void BtnRelaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var app = Application.Current as App;
                    app?.ReleaseMutexForRelaunch();
                    
                    Process.Start(
                        new ProcessStartInfo { FileName = exePath, UseShellExecute = true }
                    );
                    
                    await Task.Delay(500);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to relaunch application: " + ex.Message);
            }
        }

        #endregion
    }
}
