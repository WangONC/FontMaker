using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Media.Imaging;
using FontMaker.Utils;

namespace FontMaker
{
    /// <summary>
    /// 字符渲染器 - 负责将字符渲染为点阵位图
    /// </summary>
    public class CharacterRenderer : IDisposable
    {
        private Graphics _graphics = null!;
        private Bitmap _bitmap = null!;
        private System.Drawing.Font _font = null!;
        private SolidBrush _textBrush = null!;
        private SolidBrush _backgroundBrush = null!;
        private readonly object _lockObject = new object();
        private FontUtils? _fontUtils; // 用于检查字符支持的工具类

        // 渲染参数
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FontSize { get; private set; }
        public System.Drawing.FontFamily FontFamily { get; private set; }
        public System.Drawing.FontStyle FontStyle { get; private set; }
        public int HorizontalOffset { get; set; } = 0;
        public int VerticalOffset { get; set; } = 0;

        public CharacterRenderer(int width, int height, System.Drawing.FontFamily fontFamily, int fontSize, System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular)
        {
            Width = width;
            Height = height;
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontStyle = fontStyle;

            InitializeRenderer();
        }

        private void InitializeRenderer()
        {
            // 创建位图和Graphics对象
            _bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            _graphics = Graphics.FromImage(_bitmap);

            // 根据像素字体模式配置渲染参数
            ConfigureGraphicsSettings();

            // 创建字体
            CreateOptimalFont();

            // 创建画刷
            _textBrush = new SolidBrush(Color.White);     // 白色文字
            _backgroundBrush = new SolidBrush(Color.Black); // 黑色背景
        }

        /// <summary>
        /// 配置Graphics渲染设置
        /// </summary>
        private void ConfigureGraphicsSettings()
        {
            // 统一的渲染设置，适用于所有字体类型
            _graphics.SmoothingMode = SmoothingMode.None;
            _graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            _graphics.PixelOffsetMode = PixelOffsetMode.None;
            _graphics.CompositingQuality = CompositingQuality.HighSpeed;
            _graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
            _graphics.TextContrast = 0;
        }

        /// <summary>
        /// 创建字体对象
        /// </summary>
        private void CreateOptimalFont()
        {
            // 使用Pixel单位确保精确尺寸
            _font = new System.Drawing.Font(FontFamily, FontSize, FontStyle, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 设置WPF Typeface用于字符支持检查
        /// </summary>
        /// <param name="typeface">WPF Typeface对象</param>
        public void SetWpfTypeface(System.Windows.Media.Typeface typeface)
        {
            if (typeface != null)
            {
                _fontUtils = new FontUtils(typeface);
            }
        }

        /// <summary>
        /// 检查字体是否支持指定字符
        /// </summary>
        /// <param name="character">要检查的字符</param>
        /// <returns>如果字体支持该字符返回true，否则返回false</returns>
        public bool IsSupportedByFont(char character)
        {
            // 如果有 FontUtils 实例，使用它来检查字符支持
            if (_fontUtils != null)
            {
                return _fontUtils.SupportsCharacter(character);
            }

            // 如果没有 FontUtils，默认返回 true（保持原有行为）
            return true;
        }

        /// <summary>
        /// 渲染字符到位图
        /// </summary>
        /// <param name="character">要渲染的字符</param>
        /// <returns>渲染后的位图数据</returns>
        public BitmapSource RenderCharacter(char character)
        {
            lock (_lockObject)
            {
                // 清除背景
                _graphics.Clear(Color.Black);

                // 始终尝试渲染字符，让字符支持性检查在更高层处理
                // 测量字符尺寸
                SizeF charSize = _graphics.MeasureString(character.ToString(), _font);
                
                // 计算居中位置（考虑偏移）
                float x = (Width - charSize.Width) / 2.0f + HorizontalOffset;
                float y = (Height - charSize.Height) / 2.0f + VerticalOffset;

                // 渲染字符
                _graphics.DrawString(character.ToString(), _font, _textBrush, x, y);

                // 转换为WPF BitmapSource
                return ConvertToBitmapSource(_bitmap);
            }
        }

        /// <summary>
        /// 渲染字符到位图（支持灰度级别预览）
        /// </summary>
        /// <param name="character">要渲染的字符</param>
        /// <param name="bitsPerPixel">位深度，用于灰度级别</param>
        /// <returns>渲染后的位图数据</returns>
        public BitmapSource RenderCharacterWithGrayLevel(char character, int bitsPerPixel = 1)
        {
            lock (_lockObject)
            {
                // 清除背景
                _graphics.Clear(Color.Black);

                // 始终渲染字符，让字符支持性检查在更高层处理

                // 对于灰度渲染，启用抗锯齿以产生灰度像素
                bool useAntiAlias = bitsPerPixel > 1;
                if (useAntiAlias)
                {
                    _graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    _graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                }

                // 测量字符尺寸
                SizeF charSize = _graphics.MeasureString(character.ToString(), _font);
                
                // 计算居中位置（考虑偏移）
                float x = (Width - charSize.Width) / 2.0f + HorizontalOffset;
                float y = (Height - charSize.Height) / 2.0f + VerticalOffset;

                // 渲染字符
                _graphics.DrawString(character.ToString(), _font, _textBrush, x, y);

                // 恢复原始设置
                if (useAntiAlias)
                {
                    ConfigureGraphicsSettings();
                }

                // 如果是1位深度，直接返回原图
                if (bitsPerPixel == 1)
                {
                    return ConvertToBitmapSource(_bitmap);
                }

                // 应用灰度级别处理
                return ApplyGrayLevelToBitmap(_bitmap, bitsPerPixel);
            }
        }

        /// <summary>
        /// 渲染空白画布
        /// </summary>
        /// <returns>空白位图</returns>
        public BitmapSource RenderEmptyCanvas()
        {
            lock (_lockObject)
            {
                // 清除背景（显示为空白）
                _graphics.Clear(Color.Black);
                
                // 直接返回空白位图
                return ConvertToBitmapSource(_bitmap);
            }
        }

        /// <summary>
        /// 获取字符的像素数据数组
        /// </summary>
        /// <param name="character">要渲染的字符</param>
        /// <param name="bitsPerPixel">位深度 (1, 2, 4, 8)</param>
        /// <returns>像素数据数组</returns>
        public byte[] GetCharacterPixelData(char character, int bitsPerPixel = 1)
        {
            lock (_lockObject)
            {
                // 渲染字符
                RenderCharacter(character);

                // 锁定位图数据
                BitmapData bitmapData = _bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, Width, Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    // 提取像素数据
                    return ExtractPixelData(bitmapData, bitsPerPixel);
                }
                finally
                {
                    _bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <summary>
        /// 获取字符实际宽度
        /// </summary>
        /// <param name="character">字符</param>
        /// <returns>实际宽度（像素）</returns>
        public int GetCharacterWidth(char character)
        {
            lock (_lockObject)
            {
                SizeF charSize = _graphics.MeasureString(character.ToString(), _font);
                return (int)Math.Ceiling(charSize.Width);
            }
        }

        /// <summary>
        /// 更新渲染参数
        /// </summary>
        public void UpdateParameters(int width, int height, System.Drawing.FontFamily fontFamily, int fontSize, System.Drawing.FontStyle fontStyle)
        {
            lock (_lockObject)
            {
                bool needRecreate = (width != Width || height != Height);

                Width = width;
                Height = height;
                FontFamily = fontFamily;
                FontSize = fontSize;
                FontStyle = fontStyle;

                if (needRecreate)
                {
                    // 重新创建位图和Graphics
                    _graphics?.Dispose();
                    _bitmap?.Dispose();

                    _bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                    _graphics = Graphics.FromImage(_bitmap);
                    
                    // 重新配置渲染参数
                    ConfigureGraphicsSettings();
                }

                // 更新字体
                _font?.Dispose();
                CreateOptimalFont();
            }
        }

        /// <summary>
        /// 提取像素数据
        /// </summary>
        private byte[] ExtractPixelData(BitmapData bitmapData, int bitsPerPixel)
        {
            int stride = bitmapData.Stride;
            IntPtr scan0 = bitmapData.Scan0;
            
            // 计算输出数据大小 - 紧密打包，不按行对齐
            int totalPixels = Width * Height;
            int totalBits = totalPixels * bitsPerPixel;
            int totalBytes = (totalBits + 7) / 8;
            byte[] pixelData = new byte[totalBytes];

            int maxGrayValue = (1 << bitsPerPixel) - 1;

            // 使用Marshal.Copy来安全地读取像素数据
            byte[] scanlineData = new byte[stride];
            int pixelsPerByte = 8 / bitsPerPixel;
            int currentPixelIndex = 0;
            
            for (int y = 0; y < Height; y++)
            {
                IntPtr scanlinePtr = IntPtr.Add(scan0, y * stride);
                System.Runtime.InteropServices.Marshal.Copy(scanlinePtr, scanlineData, 0, stride);
                
                for (int x = 0; x < Width; x++)
                {
                    // 获取32位ARGB像素
                    int pixelOffset = x * 4;
                    if (pixelOffset + 2 < scanlineData.Length)
                    {
                        byte b = scanlineData[pixelOffset];
                        byte g = scanlineData[pixelOffset + 1];
                        byte r = scanlineData[pixelOffset + 2];

                        // 转换为灰度值 (使用标准权重)
                        int gray = (int)(0.299 * r + 0.587 * g + 0.114 * b);
                        
                        // 量化到指定位深度
                        int quantizedGray = (gray * maxGrayValue) / 255;

                        // 紧密打包到输出数组
                        if (currentPixelIndex < totalPixels)
                        {
                            int byteIndex = currentPixelIndex / pixelsPerByte;
                            int bitOffset = (currentPixelIndex % pixelsPerByte) * bitsPerPixel;
                            
                            if (byteIndex < pixelData.Length)
                            {
                                pixelData[byteIndex] |= (byte)(quantizedGray << (8 - bitOffset - bitsPerPixel));
                            }
                        }
                        
                        currentPixelIndex++;
                    }
                }
            }

            return pixelData;
        }

        /// <summary>
        /// 应用灰度级别处理到位图
        /// </summary>
        private BitmapSource ApplyGrayLevelToBitmap(Bitmap originalBitmap, int bitsPerPixel)
        {
            // 创建新的位图
            var processedBitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            
            // 锁定原始位图数据
            BitmapData originalData = originalBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, Width, Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            // 锁定处理后位图数据
            BitmapData processedData = processedBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, Width, Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int maxGrayValue = (1 << bitsPerPixel) - 1;
                byte[] originalBytes = new byte[originalData.Stride];
                byte[] processedBytes = new byte[processedData.Stride];

                for (int y = 0; y < Height; y++)
                {
                    // 读取原始行数据
                    IntPtr originalPtr = IntPtr.Add(originalData.Scan0, y * originalData.Stride);
                    System.Runtime.InteropServices.Marshal.Copy(originalPtr, originalBytes, 0, originalData.Stride);

                    // 处理每个像素
                    for (int x = 0; x < Width; x++)
                    {
                        int pixelOffset = x * 4;
                        if (pixelOffset + 2 < originalBytes.Length)
                        {
                            byte b = originalBytes[pixelOffset];
                            byte g = originalBytes[pixelOffset + 1];
                            byte r = originalBytes[pixelOffset + 2];
                            byte a = originalBytes[pixelOffset + 3];

                            // 转换为灰度值
                            int gray = (int)(0.299 * r + 0.587 * g + 0.114 * b);
                            
                            // 量化到指定位深度
                            int quantizedGray = (gray * maxGrayValue) / 255;
                            
                            // 转换回0-255范围
                            byte finalGray = (byte)((quantizedGray * 255) / maxGrayValue);

                            // 设置处理后的像素值（灰度）
                            processedBytes[pixelOffset] = finalGray;     // B
                            processedBytes[pixelOffset + 1] = finalGray; // G
                            processedBytes[pixelOffset + 2] = finalGray; // R
                            processedBytes[pixelOffset + 3] = a;         // A
                        }
                    }

                    // 写入处理后的行数据
                    IntPtr processedPtr = IntPtr.Add(processedData.Scan0, y * processedData.Stride);
                    System.Runtime.InteropServices.Marshal.Copy(processedBytes, 0, processedPtr, processedData.Stride);
                }
            }
            finally
            {
                originalBitmap.UnlockBits(originalData);
                processedBitmap.UnlockBits(processedData);
            }

            return ConvertToBitmapSource(processedBitmap);
        }

        /// <summary>
        /// 转换Bitmap到BitmapSource
        /// </summary>
        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _graphics?.Dispose();
                _bitmap?.Dispose();
                _font?.Dispose();
                _textBrush?.Dispose();
                _backgroundBrush?.Dispose();
            }
        }
    }

    /// <summary>
    /// 字符渲染结果
    /// </summary>
    public class CharacterRenderResult
    {
        public BitmapSource PreviewImage { get; set; } = null!;
        public byte[] PixelData { get; set; } = null!;
        public int ActualWidth { get; set; }
        public char Character { get; set; }
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }
    }
}