namespace findrbordr_native
{
    public partial class MainWindow
    {
        #region ==================== WINDOW OVERLAY POSITION & SYNC ====================

        private void SyncOverlayPosition()
        {
            SyncOverlayPositionFast();
            RefreshTitleThrottled();
        }

        private void SyncOverlayPositionFast()
        {
            if (!IsExplorerWindowValid())
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
                    targetExplorerHwnd,
                    rect.Left - 7,
                    rect.Top - 7,
                    width + 14,
                    height + 14,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOCOPYBITS | SWP_DEFERERASE
                );
            }
        }

        private void EnsureOverlayZOrder()
        {
            if (!IsExplorerWindowValid())
                return;

            IntPtr overlayHwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(
                overlayHwnd,
                targetExplorerHwnd,
                0, 0, 0, 0,
                SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
            );
        }

        private void SyncOverlayPositionInternal()
        {
            SyncOverlayPositionFast();
            RefreshTitleThrottled();
        }

        private void RefreshTitleThrottled()
        {
            if (isTitleRefreshQueued)
                return;

            isTitleRefreshQueued = true;
            Task.Run(() =>
            {
                try
                {
                    IntPtr currentTarget = targetExplorerHwnd;
                    if (currentTarget == IntPtr.Zero || !IsWindow(currentTarget))
                    {
                        isTitleRefreshQueued = false;
                        return;
                    }

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
                            UpdateActiveSidebarItem(folderName);
                        }),
                        DispatcherPriority.Background
                    );
                }
                catch
                {
                    isTitleRefreshQueued = false;
                }
            });
        }

        private string GetExplorerFolderName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return "Explorer";

            sbTitle.Clear();
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
                // Only force focus to Explorer when not in native navpane mode
                if (appSettings.NativeNavPane != 1)
                {
                    if (targetExplorerHwnd != IntPtr.Zero && IsWindow(targetExplorerHwnd))
                    {
                        SetForegroundWindow(targetExplorerHwnd);
                    }
                }
                handled = true;
                return (IntPtr)MA_NOACTIVATE;
            }
            return IntPtr.Zero;
        }

        private void Toolbar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        }

        /// <summary>
        /// Memeriksa semua tombol di sidebar dan menandai tombol yang jalurnya/namanya sesuai dengan tab/folder aktif
        /// </summary>
        private void UpdateActiveSidebarItem(string rawFolderName)
        {
            if (SidebarContainer == null)
                return;

            var buttons = FindVisualChildren<Button>(SidebarContainer);

            foreach (var btn in buttons)
            {
                if (
                    btn.Name == "DotClose"
                    || btn.Name == "DotMinimize"
                    || btn.Name == "DotMaximize"
                )
                {
                    continue;
                }
                bool isActive = IsButtonMatchingFolder(btn, rawFolderName);

                if (isActive)
                {
                    btn.Background = CachedActiveBrush;
                    btn.Foreground = CachedActiveForegroundBrush;
                }
                else
                {
                    btn.Background = Brushes.Transparent;
                    btn.SetResourceReference(Button.ForegroundProperty, "MainTextBrush");
                }
            }
        }

        private static readonly SolidColorBrush CachedActiveBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#20888888")
        );
        private static readonly SolidColorBrush CachedActiveForegroundBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#1678d4")
        );

        /// <summary>
        /// Memeriksa apakah Button sesuai dengan folder yang sedang dibuka
        /// </summary>
        private bool IsButtonMatchingFolder(Button btn, string currentFolder)
        {
            if (string.IsNullOrWhiteSpace(currentFolder))
                return false;

            string btnContent = btn.Content?.ToString() ?? "";
            string btnTag = btn.Tag?.ToString() ?? "";

            // 1. Cek berdasarkan teks tombol (Content)
            if (string.Equals(btnContent, currentFolder, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. Cek berdasarkan Tag/Path (Ambil nama folder terakhir dari path)
            if (!string.IsNullOrEmpty(btnTag))
            {
                try
                {
                    string folderFromTag = System.IO.Path.GetFileName(btnTag.TrimEnd('\\', '/'));
                    if (
                        string.Equals(
                            folderFromTag,
                            currentFolder,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                        return true;
                }
                catch { }
            }

            // 3. Khusus Special Folder / Special Tag
            if (
                btnTag.Equals("Desktop", StringComparison.OrdinalIgnoreCase)
                && currentFolder.Equals("Desktop", StringComparison.OrdinalIgnoreCase)
            )
                return true;

            if (
                btnTag.Equals("MyDocuments", StringComparison.OrdinalIgnoreCase)
                && (
                    currentFolder.Equals("Documents", StringComparison.OrdinalIgnoreCase)
                    || currentFolder.Equals("My Documents", StringComparison.OrdinalIgnoreCase)
                )
            )
                return true;

            if (
                btnTag.Equals("UserProfile", StringComparison.OrdinalIgnoreCase)
                && currentFolder.Equals(UserProfileName, StringComparison.OrdinalIgnoreCase)
            )
                return true;

            if (
                btnTag.Contains("20D04FE0-3AEA-1069-A2D8-08002B30309D")
                && (
                    currentFolder.Equals("This PC", StringComparison.OrdinalIgnoreCase)
                    || currentFolder.Equals("Computer", StringComparison.OrdinalIgnoreCase)
                )
            )
                return true;

            return false;
        }

        #endregion
    }
}
