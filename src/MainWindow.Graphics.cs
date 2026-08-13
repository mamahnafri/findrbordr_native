namespace findrbordr_native
{
    public partial class MainWindow
    {
        #region ==================== CACHED ANIMATION OBJECTS ====================

        private static readonly QuadraticEase SharedEasingFunction = new QuadraticEase
        {
            EasingMode = EasingMode.EaseOut,
        };
        private static readonly Duration AnimationDuration = TimeSpan.FromSeconds(0.25);
        private readonly DoubleAnimation _cachedOpacityAnimation = new DoubleAnimation
        {
            Duration = AnimationDuration,
            EasingFunction = SharedEasingFunction,
        };
        private readonly ColorAnimation _cachedColorAnimation = new ColorAnimation
        {
            Duration = AnimationDuration,
            EasingFunction = SharedEasingFunction,
        };

        #endregion

        #region ==================== GRAPHICS & PARALLAX EFFECT ====================

private BitmapSource ApplyMacOsFrostedGlass(BitmapSource original, int blurRadius = 15, float macSat = 1.6f, float macContrast = 1.15f)
{
    int width = original.PixelWidth;
    int height = original.PixelHeight;

    FormatConvertedBitmap converted = new(original, PixelFormats.Bgra32, null, 0);
    WriteableBitmap wBitmap = new(converted);

    wBitmap.Lock();
    unsafe
    {
        byte* ptr = (byte*)wBitmap.BackBuffer.ToPointer();
        if (ptr == null)
        {
            wBitmap.Unlock();
            return wBitmap;
        }
        
        int stride = wBitmap.BackBufferStride;

        // Buffer sementara untuk 2-Pass Separable Blur
        byte* temp1 = (byte*)NativeMemory.Alloc((nuint)(height * stride));
        byte* temp2 = (byte*)NativeMemory.Alloc((nuint)(height * stride));
        
        if (temp1 == null || temp2 == null)
        {
            if (temp1 != null) NativeMemory.Free(temp1);
            if (temp2 != null) NativeMemory.Free(temp2);
            wBitmap.Unlock();
            return wBitmap;
        }

        Buffer.MemoryCopy(ptr, temp1, height * stride, height * stride);

        int radius = Math.Max(1, blurRadius);

        // Edge Mirroring agar tepi tidak vignette/gelap
        static int Reflect(int p, int max)
        {
            if (p < 0) return -p;
            if (p >= max) return 2 * max - p - 1;
            return p;
        }

        // ==========================================
        // PASS 1: Horizontal Blur (temp1 -> temp2)
        // ==========================================
        for (int y = 0; y < height; y++)
        {
            int rowIdx = y * stride;
            for (int x = 0; x < width; x++)
            {
                int rSum = 0, gSum = 0, bSum = 0, count = 0;
                for (int kx = -radius; kx <= radius; kx++)
                {
                    int nx = Reflect(x + kx, width);
                    int idx = rowIdx + nx * 4;
                    bSum += temp1[idx];
                    gSum += temp1[idx + 1];
                    rSum += temp1[idx + 2];
                    count++;
                }
                int outIdx = rowIdx + x * 4;
                temp2[outIdx]     = (byte)(bSum / count);
                temp2[outIdx + 1] = (byte)(gSum / count);
                temp2[outIdx + 2] = (byte)(rSum / count);
            }
        }

        // ==========================================
        // PASS 2: Vertical Blur + macOS Vibrancy Adjustments (temp2 -> ptr)
        // ==========================================

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int rSum = 0, gSum = 0, bSum = 0, count = 0;
                for (int ky = -radius; ky <= radius; ky++)
                {
                    int ny = Reflect(y + ky, height);
                    int idx = ny * stride + x * 4;
                    bSum += temp2[idx];
                    gSum += temp2[idx + 1];
                    rSum += temp2[idx + 2];
                    count++;
                }

                float b = bSum / (float)count;
                float g = gSum / (float)count;
                float r = rSum / (float)count;

                // 2. Normalisasi 0..1
                r /= 255f; g /= 255f; b /= 255f;

                // 3. Kontras Adjustment
                r = (macContrast * (r * 255f - 128f) + 128f) / 255f;
                g = (macContrast * (g * 255f - 128f) + 128f) / 255f;
                b = (macContrast * (b * 255f - 128f) + 128f) / 255f;

                // 4. Vibrancy / Saturation Boost
                float gray = 0.299f * r + 0.587f * g + 0.114f * b;
                r = gray + (r - gray) * macSat;
                g = gray + (g - gray) * macSat;
                b = gray + (b - gray) * macSat;

                // 5. Tulis balik ke back buffer
                int idxOut = y * stride + x * 4;
                ptr[idxOut]     = (byte)Math.Clamp(b * 255f, 0f, 255f);
                ptr[idxOut + 1] = (byte)Math.Clamp(g * 255f, 0f, 255f);
                ptr[idxOut + 2] = (byte)Math.Clamp(r * 255f, 0f, 255f);
            }
        }

        // Bebaskan alokasi memori
        NativeMemory.Free(temp1);
        NativeMemory.Free(temp2);

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
        if (wallpaperBrush != null && ParallaxCanvas != null)
        {
            ParallaxCanvas.Background = null;
            wallpaperBrush = null;
        }

        BitmapSource? processedBitmap = await Task.Run(() =>
        {
            StringBuilder wallPaperPath = new(260);
            SystemParametersInfo(
                SPI_GETDESKWALLPAPER,
                (uint)wallPaperPath.Capacity,
                wallPaperPath,
                0
            );

            string path = wallPaperPath.ToString();
            if (!File.Exists(path))
                return null;

            try
            {
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 256;
                bitmap.EndInit();
                bitmap.Freeze();

                // Cukup 1 kali panggil metode khusus macOS ini!
                // Radius 15-25 sangat direkomendasikan untuk style Finder/Sidebar macOS
                BitmapSource result = ApplyMacOsFrostedGlass(bitmap, blurRadius: 20, macSat: 2.0f, macContrast: 1.25f);

                return result;
            }
            catch
            {
                return null;
            }
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
                Viewport = new Rect(0, 0, screenWidth, screenHeight),
            };

            ParallaxCanvas.Background = wallpaperBrush;

            _ = Dispatcher.BeginInvoke(
                new Action(() => UpdateParallaxOffset()),
                DispatcherPriority.Loaded
            );
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

private void ReleaseWallpaperResources()
{
    if (wallpaperBrush != null)
    {
        if (ParallaxCanvas != null)
            ParallaxCanvas.Background = null;
        wallpaperBrush = null;
    }
}

        #endregion

        #region ==================== OVERLAY FADE EFFECTS ====================

        private void FadeOutOverlay()
        {
            try
            {
                if (this.Content is FrameworkElement root)
                {
                    root.BeginAnimation(UIElement.OpacityProperty, null);
                    root.Opacity = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FadeOutOverlay error: {ex.Message}");
            }
        }

        private void FadeInOverlay()
        {
            try
            {
                if (this.Content is FrameworkElement root)
                {
                    _cachedOpacityAnimation.To = 1;
                    root.BeginAnimation(UIElement.OpacityProperty, _cachedOpacityAnimation);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FadeInOverlay error: {ex.Message}");
            }
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

            double clampedOpacity = Math.Max(0.0, Math.Min(1.0, targetOpacity));
            _cachedOpacityAnimation.To = clampedOpacity;
            element.BeginAnimation(UIElement.OpacityProperty, _cachedOpacityAnimation);
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

        private CancellationTokenSource? _altTabRetryCts;

        private void UpdateExplorerFocusState(IntPtr activeHwnd)
        {
            try
            {
                var oldCts = _altTabRetryCts;
                _altTabRetryCts = new CancellationTokenSource();
                oldCts?.Cancel();
                oldCts?.Dispose();
                
                var token = _altTabRetryCts.Token;

                Dispatcher.BeginInvoke(
                    new Action(async () =>
                    {
                        try
                        {
                            if (token.IsCancellationRequested)
                                return;

                            IntPtr currentForeground = GetForegroundWindow();
                            bool isExplorerFocused = EvaluateFocus(currentForeground, activeHwnd);

                            if (!isExplorerFocused && lastExplorerFocusVisualState)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    await Task.Delay(60, token).ConfigureAwait(true);
                                    if (token.IsCancellationRequested)
                                        return;

                                    currentForeground = GetForegroundWindow();
                                    if (EvaluateFocus(currentForeground, IntPtr.Zero))
                                    {
                                        isExplorerFocused = true;
                                        break;
                                    }
                                }
                            }

                            if (isExplorerFocused == lastExplorerFocusVisualState)
                                return;

                            lastExplorerFocusVisualState = isExplorerFocused;

                            ApplyFocusVisualState(isExplorerFocused);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Error in Focus Handler: {ex.Message}"
                            );
                        }
                    }),
                    DispatcherPriority.Input
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error queueing focus update: {ex.Message}");
            }
        }

        private bool EvaluateFocus(IntPtr foregroundHwnd, IntPtr activeHwnd)
        {
            if (targetExplorerHwnd == IntPtr.Zero)
                return false;

            bool matchesTarget =
                (foregroundHwnd == targetExplorerHwnd) || (activeHwnd == targetExplorerHwnd);
            return matchesTarget && IsWindowVisible(targetExplorerHwnd);
        }

        private void ApplyFocusVisualState(bool isFocused)
        {
            try
            {
                if (this.Content is not FrameworkElement root)
                    return;

                var dotClose = FindElementByName<Button>(root, "DotClose");
                var dotMinimize = FindElementByName<Button>(root, "DotMinimize");
                var dotMaximize = FindElementByName<Button>(root, "DotMaximize");
                var layer3Border = FindElementByName<Border>(root, "Layer3Border");

                if (isFocused)
                {
                    if (dotClose != null)
                        AnimateBackground(dotClose, "#FF5F56");
                    if (dotMinimize != null)
                        AnimateBackground(dotMinimize, "#FFBD2E");
                    if (dotMaximize != null)
                        AnimateBackground(dotMaximize, "#27C93F");
                    if (layer3Border != null)
                    {
                        double targetOpacity = appSettings?.Layer3BorderBackground?.Opacity ?? 0.8;
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying visual state: {ex.Message}");
            }
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

            _cachedColorAnimation.To = targetColor;
            currentBrush.BeginAnimation(SolidColorBrush.ColorProperty, _cachedColorAnimation);
        }

        #endregion
    }
}
