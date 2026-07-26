namespace findrbordr_native
{
    public partial class MainWindow
    {
        #region ==================== CACHED ANIMATION OBJECTS ====================

        private static readonly QuadraticEase SharedEasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        private static readonly Duration AnimationDuration = TimeSpan.FromSeconds(0.25);
        private readonly DoubleAnimation _cachedOpacityAnimation = new DoubleAnimation { Duration = AnimationDuration, EasingFunction = SharedEasingFunction };
        private readonly ColorAnimation _cachedColorAnimation = new ColorAnimation { Duration = AnimationDuration, EasingFunction = SharedEasingFunction };

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

                    if (gray <= 0.31f)
                    {
                        r = 0.90f;
                        g = 0.90f;
                        b = 0.90f;
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

        private BitmapSource ApplyGaussianBlur(BitmapSource source, int radius)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;

            FormatConvertedBitmap converted = new FormatConvertedBitmap(
                source,
                PixelFormats.Bgra32,
                null,
                0
            );
            WriteableBitmap wBitmap = new WriteableBitmap(converted);

            if (radius < 1)
            {
                wBitmap.Freeze();
                return wBitmap;
            }

            int kernelSize = radius * 2 + 1;
            float sigma = radius / 3f;
            float[] kernel = new float[kernelSize];
            float sum = 0;

            for (int i = 0; i < kernelSize; i++)
            {
                float x = i - radius;
                kernel[i] = (float)Math.Exp(-(x * x) / (2 * sigma * sigma));
                sum += kernel[i];
            }

            for (int i = 0; i < kernelSize; i++)
                kernel[i] /= sum;

            WriteableBitmap tempBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            wBitmap.Lock();
            tempBitmap.Lock();

            unsafe
            {
                byte* srcPtr = (byte*)wBitmap.BackBuffer.ToPointer();
                byte* dstPtr = (byte*)tempBitmap.BackBuffer.ToPointer();
                int srcStride = wBitmap.BackBufferStride;
                int dstStride = tempBitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float r = 0, g = 0, b = 0, a = 0;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int px = Math.Clamp(x + k, 0, width - 1);
                            int offset = y * srcStride + px * 4;

                            float weight = kernel[k + radius];
                            b += srcPtr[offset] * weight;
                            g += srcPtr[offset + 1] * weight;
                            r += srcPtr[offset + 2] * weight;
                            a += srcPtr[offset + 3] * weight;
                        }

                        int dstOffset = y * dstStride + x * 4;
                        dstPtr[dstOffset] = (byte)Math.Clamp(b, 0, 255);
                        dstPtr[dstOffset + 1] = (byte)Math.Clamp(g, 0, 255);
                        dstPtr[dstOffset + 2] = (byte)Math.Clamp(r, 0, 255);
                        dstPtr[dstOffset + 3] = (byte)Math.Clamp(a, 0, 255);
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float r = 0, g = 0, b = 0, a = 0;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int py = Math.Clamp(y + k, 0, height - 1);
                            int offset = py * dstStride + x * 4;

                            float weight = kernel[k + radius];
                            b += dstPtr[offset] * weight;
                            g += dstPtr[offset + 1] * weight;
                            r += dstPtr[offset + 2] * weight;
                            a += dstPtr[offset + 3] * weight;
                        }

                        int srcOffset = y * srcStride + x * 4;
                        srcPtr[srcOffset] = (byte)Math.Clamp(b, 0, 255);
                        srcPtr[srcOffset + 1] = (byte)Math.Clamp(g, 0, 255);
                        srcPtr[srcOffset + 2] = (byte)Math.Clamp(r, 0, 255);
                        srcPtr[srcOffset + 3] = (byte)Math.Clamp(a, 0, 255);
                    }
                }

                wBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }

            tempBitmap.Unlock();
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

                    BitmapImage? bitmap = null;
                    try
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 256;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        BitmapSource result = AdjustContrastAndSaturation(
                            bitmap,
                            contrast: 1.0f,
                            saturation: 3.0f
                        );
                        
                        result = ApplyGaussianBlur(result, radius: 10);
                        
                        if (result.CanFreeze)
                            result.Freeze();

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
                        Viewport = new Rect(0, 0, screenWidth, screenHeight)
                    };

                    ParallaxCanvas.Background = wallpaperBrush;
                    
                    Dispatcher.BeginInvoke(
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

            _cachedColorAnimation.From = currentBrush.Color;
            _cachedColorAnimation.To = targetColor;
            currentBrush.BeginAnimation(SolidColorBrush.ColorProperty, _cachedColorAnimation);
        }

        

        #endregion
    }
}
