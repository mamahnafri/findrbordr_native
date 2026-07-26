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

            if (
                sender is Button btn
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

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            // Buka ContextMenu tepat di tombol overlay yang diklik!
            if (sender is FrameworkElement element && element.ContextMenu != null)
            {
                element.ContextMenu.IsOpen = true;
            }
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
                ApplySidebarVisibility();
                ApplyOuterCosmeticBorderVisibility();
                ApplyToolbarPosition();

                if (
                    this.FindName("SidebarColumn") is ColumnDefinition sidebarCol
                    && appSettings.SidebarWidth > 0
                )
                {
                    sidebarCol.Width = new GridLength(appSettings.SidebarWidth);
                }

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gagal menerapkan UI settings: " + ex.Message);
            }
        }

        private void ApplyOuterCosmeticBorderVisibility()
        {
            bool showBorder = appSettings.OuterCosmeticBorderVisible != 0;

            if (this.FindName("OuterCosmeticFrameBorder") is Border outerBorder)
                outerBorder.Visibility = showBorder ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyToolbarPosition()
        {
            if (this.FindName("ToolbarGrid") is Grid toolbarGrid)
            {
                toolbarGrid.RenderTransform = new TranslateTransform(appSettings.ToolbarPosX, appSettings.ToolbarPosY);
            }
        }

        private void ApplySidebarVisibility()
        {
            bool showSidebar = appSettings.SidebarVisible != 0;

            // 1. Ubah Lebar Kolom
            if (this.FindName("SidebarColumn") is ColumnDefinition sidebarCol)
            {
                sidebarCol.Width = showSidebar
                    ? new GridLength(appSettings.SidebarWidth)
                    : new GridLength(50);
            }

            // 2. Sembunyikan SELURUH Layer Sidebar saat ditutup (Mac Dot TIDAK AKAN HILANG)
            if (this.FindName("LeftSidebarShadowBorder") is Border shadowBorder)
                shadowBorder.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;

            if (this.FindName("SidebarGlassBorder") is Border glassBorder)
                glassBorder.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;

            if (this.FindName("Layer3Border") is Border layer3Border)
                layer3Border.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;
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
    }
}
