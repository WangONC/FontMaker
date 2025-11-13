using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using FontMaker.Data;
using FontMaker.Data.Models;
using FontRenderStatistics = FontMaker.Data.Models.FontRenderStatistics;

namespace FontMaker
{
    /// <summary>
    /// 点阵字体渲染管理器 - 管理字符渲染和数据提取
    /// </summary>
    public class BitmapFontRenderer : IDisposable
    {
        private CharacterRenderer _renderer;
        
        // 渲染参数
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string FontFamily { get; private set; }
        public int FontSize { get; private set; }
        public System.Drawing.FontStyle FontStyle { get; private set; }
        public int HorizontalOffset { get; set; } = 0;
        public int VerticalOffset { get; set; } = 0;
        public int BitsPerPixel { get; set; } = 1;

        // 扫描和编码参数
        public bool IsHorizontalScan { get; set; } = true;
        public bool IsHighBitFirst { get; set; } = true; // true=从左到右，false=从右到左
        public bool IsFixedWidth { get; set; } = true;

        public BitmapFontRenderer(int width, int height, string fontFamily, int fontSize, System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular)
        {
            Width = width;
            Height = height;
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontStyle = fontStyle;

            // 创建 System.Drawing.FontFamily 对象
            var gdiFontFamily = CreateGdiFontFamily(fontFamily);
            _renderer = new CharacterRenderer(width, height, gdiFontFamily, fontSize, fontStyle);
            
            // 创建对应的 WPF Typeface 用于字符支持检查
            var wpfTypeface = CreateWpfTypeface(fontFamily, fontStyle);
            if (wpfTypeface != null)
            {
                _renderer.SetWpfTypeface(wpfTypeface);
            }
        }

        /// <summary>
        /// 创建对应的 WPF Typeface 用于字符支持检查
        /// </summary>
        /// <param name="fontFamilyName">字体名称</param>
        /// <param name="fontStyle">字体样式</param>
        /// <returns>WPF Typeface对象</returns>
        private System.Windows.Media.Typeface? CreateWpfTypeface(string fontFamilyName, System.Drawing.FontStyle fontStyle)
        {
            try
            {
                // 创建 WPF FontFamily
                var wpfFontFamily = new System.Windows.Media.FontFamily(fontFamilyName);
                
                // 转换 GDI+ FontStyle 到 WPF 样式
                var wpfFontStyle = (fontStyle & System.Drawing.FontStyle.Italic) != 0 
                    ? System.Windows.FontStyles.Italic 
                    : System.Windows.FontStyles.Normal;
                    
                var wpfFontWeight = (fontStyle & System.Drawing.FontStyle.Bold) != 0 
                    ? System.Windows.FontWeights.Bold 
                    : System.Windows.FontWeights.Normal;
                
                return new System.Windows.Media.Typeface(wpfFontFamily, wpfFontStyle, wpfFontWeight, System.Windows.FontStretches.Normal);
            }
            catch (Exception)
            {
                // 如果创建失败，返回 null
                return null;
            }
        }

        /// <summary>
        /// 根据字体名称创建 GDI+ FontFamily 对象
        /// </summary>
        private System.Drawing.FontFamily CreateGdiFontFamily(string fontFamilyName)
        {
            // 检查是否为URI格式（本地导入的字体）
            if (fontFamilyName.Contains("file:///"))
            {
                // 解析URI格式：file:///path/to/font.ttf#FontName
                var parts = fontFamilyName.Split('#');
                if (parts.Length != 2)
                {
                    throw new ArgumentException(string.Format(FontMaker.Resources.Lang.Languages.CannotParseFontPath, fontFamilyName));
                }

                string fontPath = parts[0].Replace("file:///", "").Replace("/", "\\");
                string fontName = parts[1];
                
                // 使用PrivateFontCollection加载字体文件
                var fontCollection = new System.Drawing.Text.PrivateFontCollection();
                fontCollection.AddFontFile(fontPath);
                
                // 使用FontCollection构造函数创建FontFamily
                return new System.Drawing.FontFamily(fontName, fontCollection);
            }
            else
            {
                // 系统字体
                return new System.Drawing.FontFamily(fontFamilyName);
            }
        }

        /// <summary>
        /// 渲染单个字符获取预览图像
        /// </summary>
        /// <param name="character">要渲染的字符</param>
        /// <returns>预览图像</returns>
        public BitmapSource RenderCharacterPreview(char character)
        {
            // 检查字符是否被当前字体支持
            if (!_renderer.IsSupportedByFont(character))
            {
                // 如果字体不支持该字符，返回空白画布
                _renderer.HorizontalOffset = HorizontalOffset;
                _renderer.VerticalOffset = VerticalOffset;
                return _renderer.RenderEmptyCanvas();
            }
            
            _renderer.HorizontalOffset = HorizontalOffset;
            _renderer.VerticalOffset = VerticalOffset;
            return _renderer.RenderCharacterWithGrayLevel(character, BitsPerPixel);
        }

        /// <summary>
        /// 渲染字符获取完整结果
        /// </summary>
        /// <param name="character">要渲染的字符</param>
        /// <returns>渲染结果</returns>
        public CharacterRenderResult RenderCharacter(char character)
        {
            _renderer.HorizontalOffset = HorizontalOffset;
            _renderer.VerticalOffset = VerticalOffset;

            // 检查字符是否被当前字体支持
            if (!_renderer.IsSupportedByFont(character))
            {
                // 如果字体不支持该字符，返回空白结果
                var emptyImage = _renderer.RenderEmptyCanvas();
                return new CharacterRenderResult
                {
                    Character = character,
                    PreviewImage = emptyImage,
                    PixelData = new byte[(Width * Height * BitsPerPixel + 7) / 8], // 全0数据
                    ActualWidth = 1,
                    CanvasWidth = Width,
                    CanvasHeight = Height
                };
            }

            var previewImage = _renderer.RenderCharacter(character);
            var pixelData = _renderer.GetCharacterPixelData(character, BitsPerPixel);
            var actualWidth = _renderer.GetCharacterWidth(character);

            // 根据扫描方式重新组织数据
            if (!IsHorizontalScan)
            {
                pixelData = ConvertToVerticalScan(pixelData, Width, Height, BitsPerPixel) ?? pixelData;
            }

            // 根据位序要求调整
            if (!IsHighBitFirst)
            {
                pixelData = ReverseBitOrder(pixelData) ?? pixelData;
            }

            return new CharacterRenderResult
            {
                PreviewImage = previewImage,
                PixelData = pixelData,
                ActualWidth = actualWidth,
                Character = character,
                CanvasWidth = Width,
                CanvasHeight = Height
            };
        }

        /// <summary>
        /// 批量渲染字符集，生成完整的字体点阵数据
        /// </summary>
        /// <param name="charset">字符集</param>
        /// <returns>完整的字体点阵数据</returns>
        public FontBitmapData RenderCharset(CharsetManager charset)
        {
            var fontData = new FontBitmapData();
            
            // 设置元数据
            fontData.Metadata.FontFamily = FontFamily;
            fontData.Metadata.FontSize = FontSize;
            fontData.Metadata.CanvasWidth = Width;
            fontData.Metadata.CanvasHeight = Height;
            fontData.Metadata.BitsPerPixel = BitsPerPixel;
            fontData.Metadata.IsHorizontalScan = IsHorizontalScan;
            fontData.Metadata.IsHighBitFirst = IsHighBitFirst;
            fontData.Metadata.IsFixedWidth = IsFixedWidth;
            fontData.Metadata.CharsetName = charset.Name ?? "Unknown";

            // 第一次遍历：获取所有字符的基本信息，计算最大尺寸
            var tempResults = new List<(char character, byte[] rawData, int actualWidth, int actualHeight)>();
            int maxWidth = 0;
            int maxHeight = 0;

            foreach (char character in charset.Characters)
            {
                if(Config.IsRemoveUnsupportChar && !_renderer.IsSupportedByFont(character))
                {
                    // 如果不支持该字符，跳过
                    continue;
                }
                try
                {
                    var rawData = _renderer.GetCharacterPixelData(character, BitsPerPixel);
                    var actualWidth = _renderer.GetCharacterWidth(character);
                    var actualHeight = Height; // 暂时使用画布高度，后续会计算实际高度
                    
                    // 移除空白边缘并获取实际尺寸
                    var (trimmedData, trimmedWidth, trimmedHeight) = TrimEmptyEdges(rawData, Width, Height, BitsPerPixel);
                    
                    tempResults.Add((character, trimmedData, trimmedWidth, trimmedHeight));
                    
                    maxWidth = Math.Max(maxWidth, trimmedWidth);
                    maxHeight = Math.Max(maxHeight, trimmedHeight);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to render character '{character}': {ex.Message}");
                }
            }

            // 计算需要的字节数
            if (IsFixedWidth)
            {
                // 固定宽度模式：使用画布尺寸
                fontData.Metadata.MaxCharacterWidth = Width;
                fontData.Metadata.MaxCharacterHeight = Height;
                fontData.Metadata.WidthBytesCount = GetRequiredBytes(Width);
                fontData.Metadata.HeightBytesCount = GetRequiredBytes(Height);
            }
            else
            {
                // 可变宽度模式：使用实际最大尺寸
                fontData.Metadata.MaxCharacterWidth = maxWidth;
                fontData.Metadata.MaxCharacterHeight = maxHeight;
                fontData.Metadata.WidthBytesCount = GetRequiredBytes(maxWidth);
                fontData.Metadata.HeightBytesCount = GetRequiredBytes(maxHeight);
            }

            // 第二次遍历：生成最终数据
            var completeBitStream = new List<bool>();

            foreach (var (character, rawData, actualWidth, actualHeight) in tempResults)
            {
                var charResult = new CharacterBitmapResult
                {
                    Character = character,
                    ActualWidth = actualWidth,
                    ActualHeight = actualHeight,
                    CanvasWidth = Width,
                    CanvasHeight = Height
                };

                // 根据是否固定宽度决定使用的数据和尺寸
                byte[] dataToProcess;
                int dataWidth, dataHeight;
                
                if (IsFixedWidth)
                {
                    // 固定宽度模式：使用原始画布数据和尺寸（不裁剪）
                    dataToProcess = _renderer.GetCharacterPixelData(character, BitsPerPixel);
                    dataWidth = Width;
                    dataHeight = Height;
                    // 更新实际尺寸为画布尺寸
                    charResult.ActualWidth = Width;
                    charResult.ActualHeight = Height;
                }
                else
                {
                    // 可变宽度模式：使用裁剪后的数据和尺寸
                    dataToProcess = rawData;
                    dataWidth = actualWidth;
                    dataHeight = actualHeight;
                }

                // 处理扫描方式
                var processedData = IsHorizontalScan ? dataToProcess : (ConvertToVerticalScan(dataToProcess, dataWidth, dataHeight, BitsPerPixel) ?? dataToProcess);

                // 处理位序
                if (IsHighBitFirst)
                {
                    // 从左到右：需要反转默认的MSB存储格式
                    processedData = ReverseBitOrder(processedData) ?? processedData;
                }
                // 从右到左：保持默认存储格式（原始MSB实际表现为从右到左）

                // 转换为BitArray
                charResult.BitmapData = new BitArray(processedData.Length * 8);
                
                for (int i = 0; i < processedData.Length; i++)
                {
                    for (int bit = 0; bit < 8; bit++)
                    {
                        charResult.BitmapData[i * 8 + bit] = (processedData[i] & (1 << (7 - bit))) != 0;
                    }
                }

                // 生成宽高信息字节
                if (!IsFixedWidth)
                {
                    charResult.WidthBytes = GetSizeBytes(actualWidth, fontData.Metadata.WidthBytesCount);
                    charResult.HeightBytes = GetSizeBytes(actualHeight, fontData.Metadata.HeightBytesCount);
                }

                fontData.Characters.Add(charResult);

                // 添加到完整比特流
                if (!IsFixedWidth)
                {
                    AddBytesToBitStream(completeBitStream, charResult.WidthBytes);
                    AddBytesToBitStream(completeBitStream, charResult.HeightBytes);
                }
                AddBitArrayToBitStream(completeBitStream, charResult.BitmapData);
            }

            // 创建完整的BitArray
            fontData.CompleteBitStream = new BitArray(completeBitStream.ToArray());

            return fontData;
        }

        /// <summary>
        /// 移除空白边缘
        /// </summary>
        private (byte[] trimmedData, int width, int height) TrimEmptyEdges(byte[] data, int originalWidth, int originalHeight, int bitsPerPixel)
        {
            if (data == null || data.Length == 0)
                return (data ?? new byte[1], 1, 1);

            int pixelsPerByte = 8 / bitsPerPixel;
            int totalPixels = originalWidth * originalHeight;
            int totalBits = totalPixels * bitsPerPixel;
            int expectedBytes = (totalBits + 7) / 8;
            
            // 如果数据长度不足，返回最小尺寸
            if (data.Length < expectedBytes)
            {
                return (new byte[1], 1, 1);
            }

            // 找到内容边界
            int topBound = -1, bottomBound = -1, leftBound = originalWidth, rightBound = -1;

            // 逐行扫描找到上下边界
            for (int y = 0; y < originalHeight; y++)
            {
                bool hasContent = false;
                
                for (int x = 0; x < originalWidth; x++)
                {
                    int pixelIndex = y * originalWidth + x;
                    int bitIndex = pixelIndex * bitsPerPixel;
                    int byteIndex = bitIndex / 8;
                    int bitOffset = bitIndex % 8;
                    
                    if (byteIndex < data.Length)
                    {
                        int mask = ((1 << bitsPerPixel) - 1) << (8 - bitOffset - bitsPerPixel);
                        if ((data[byteIndex] & mask) != 0)
                        {
                            hasContent = true;
                            break;
                        }
                    }
                }

                if (hasContent)
                {
                    if (topBound == -1) topBound = y;
                    bottomBound = y;
                }
            }

            // 如果没有内容，返回1x1的空数据
            if (topBound == -1)
            {
                return (new byte[1], 1, 1);
            }

            // 逐列扫描找到左右边界（在有内容的行范围内）
            for (int x = 0; x < originalWidth; x++)
            {
                bool hasContent = false;
                
                for (int y = topBound; y <= bottomBound; y++)
                {
                    int pixelIndex = y * originalWidth + x;
                    int bitIndex = pixelIndex * bitsPerPixel;
                    int byteIndex = bitIndex / 8;
                    int bitOffset = bitIndex % 8;
                    
                    if (byteIndex < data.Length)
                    {
                        int mask = ((1 << bitsPerPixel) - 1) << (8 - bitOffset - bitsPerPixel);
                        if ((data[byteIndex] & mask) != 0)
                        {
                            hasContent = true;
                            break;
                        }
                    }
                }

                if (hasContent)
                {
                    if (leftBound == originalWidth) leftBound = x; // 第一次找到，设为左边界
                    rightBound = x; // 最后一次找到，更新为右边界
                }
            }

            // 如果没找到左右边界，说明没有内容
            if (rightBound == -1)
            {
                return (new byte[1], 1, 1);
            }

            // 计算裁剪后的尺寸
            int newWidth = rightBound - leftBound + 1;
            int newHeight = bottomBound - topBound + 1;
            int newTotalPixels = newWidth * newHeight;
            int newTotalBits = newTotalPixels * bitsPerPixel;
            int newTotalBytes = (newTotalBits + 7) / 8;
            
            // 提取裁剪后的数据
            byte[] trimmedData = new byte[newTotalBytes];
            
            // 逐像素复制裁剪后的数据
            for (int dstPixelIndex = 0; dstPixelIndex < newTotalPixels; dstPixelIndex++)
            {
                int newY = dstPixelIndex / newWidth;
                int newX = dstPixelIndex % newWidth;
                int srcY = topBound + newY;
                int srcX = leftBound + newX;
                
                // 源像素位置
                int srcPixelIndex = srcY * originalWidth + srcX;
                int srcBitIndex = srcPixelIndex * bitsPerPixel;
                int srcByteIndex = srcBitIndex / 8;
                int srcBitOffset = srcBitIndex % 8;
                
                // 目标像素位置
                int dstBitIndex = dstPixelIndex * bitsPerPixel;
                int dstByteIndex = dstBitIndex / 8;
                int dstBitOffset = dstBitIndex % 8;
                
                // 确保索引有效
                if (srcByteIndex < data.Length && dstByteIndex < trimmedData.Length)
                {
                    // 提取源像素值
                    int srcMask = ((1 << bitsPerPixel) - 1) << (8 - srcBitOffset - bitsPerPixel);
                    int pixelValue = (data[srcByteIndex] & srcMask) >> (8 - srcBitOffset - bitsPerPixel);
                    
                    // 清除目标位置并设置新值
                    int dstMask = ((1 << bitsPerPixel) - 1) << (8 - dstBitOffset - bitsPerPixel);
                    trimmedData[dstByteIndex] &= (byte)~dstMask; // 清除目标位
                    trimmedData[dstByteIndex] |= (byte)(pixelValue << (8 - dstBitOffset - bitsPerPixel));
                }
            }

            return (trimmedData, newWidth, newHeight);
        }

        /// <summary>
        /// 计算存储指定数值需要的字节数
        /// </summary>
        private int GetRequiredBytes(int value)
        {
            if (value <= 255) return 1;
            if (value <= 65535) return 2;
            if (value <= 16777215) return 3;
            return 4;
        }

        /// <summary>
        /// 将整数转换为指定字节数的字节数组
        /// </summary>
        private byte[] GetSizeBytes(int value, int byteCount)
        {
            var bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)((value >> (i * 8)) & 0xFF);
            }
            return bytes;
        }

        /// <summary>
        /// 将字节数组添加到比特流
        /// </summary>
        private void AddBytesToBitStream(List<bool> bitStream, byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    bitStream.Add((b & (1 << (7 - bit))) != 0);
                }
            }
        }

        /// <summary>
        /// 将BitArray添加到比特流
        /// </summary>
        private void AddBitArrayToBitStream(List<bool> bitStream, BitArray bitArray)
        {
            for (int i = 0; i < bitArray.Length; i++)
            {
                bitStream.Add(bitArray[i]);
            }
        }

        /// <summary>
        /// 更新渲染参数
        /// </summary>
        public void UpdateRenderParameters(int width, int height, string fontFamily, int fontSize, System.Drawing.FontStyle fontStyle)
        {
            Width = width;
            Height = height;
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontStyle = fontStyle;

            // 创建 System.Drawing.FontFamily 对象
            var gdiFontFamily = CreateGdiFontFamily(fontFamily);
            _renderer.UpdateParameters(width, height, gdiFontFamily, fontSize, fontStyle);
            
            // 更新对应的 WPF Typeface 用于字符支持检查
            var wpfTypeface = CreateWpfTypeface(fontFamily, fontStyle);
            if (wpfTypeface != null)
            {
                _renderer.SetWpfTypeface(wpfTypeface);
            }
        }

        /// <summary>
        /// 获取字符的二进制数据（用于文件导出）
        /// </summary>
        /// <param name="character">字符</param>
        /// <param name="includeWidthInfo">是否包含宽度信息</param>
        /// <returns>二进制数据</returns>
        public byte[] GetCharacterBinaryData(char character, bool includeWidthInfo = false)
        {
            var result = RenderCharacter(character);

            if (result.PixelData == null)
                throw new InvalidOperationException(FontMaker.Resources.Lang.Languages.CharacterRenderingFailed);

            if (!includeWidthInfo || IsFixedWidth)
            {
                return result.PixelData;
            }

            // 可变宽度模式：在数据前添加宽度信息
            var dataWithWidth = new List<byte>();
            dataWithWidth.Add((byte)result.ActualWidth); // 宽度信息
            dataWithWidth.AddRange(result.PixelData);

            return dataWithWidth.ToArray();
        }

        /// <summary>
        /// 转换为垂直扫描
        /// </summary>
        private byte[]? ConvertToVerticalScan(byte[] horizontalData, int width, int height, int bitsPerPixel)
        {
            if (horizontalData == null || horizontalData.Length == 0)
                return horizontalData;

            int pixelsPerByte = 8 / bitsPerPixel;
            int horizontalBytesPerRow = (width + pixelsPerByte - 1) / pixelsPerByte;
            int verticalBytesPerColumn = (height + pixelsPerByte - 1) / pixelsPerByte;
            
            // 垂直扫描时，数据按列排列：列1，列2，列3...
            byte[] verticalData = new byte[verticalBytesPerColumn * width];

            // 遍历每一列
            for (int col = 0; col < width; col++)
            {
                int columnStartByte = col * verticalBytesPerColumn;
                
                // 遍历该列的每一行
                for (int row = 0; row < height; row++)
                {
                    // 从水平数据中获取像素值
                    int horizontalPixelIndex = row * width + col;
                    int horizontalByteIndex = horizontalPixelIndex / pixelsPerByte;
                    int horizontalBitOffset = (horizontalPixelIndex % pixelsPerByte) * bitsPerPixel;
                    
                    if (horizontalByteIndex < horizontalData.Length)
                    {
                        // 提取像素值
                        int mask = ((1 << bitsPerPixel) - 1) << (8 - horizontalBitOffset - bitsPerPixel);
                        int pixelValue = (horizontalData[horizontalByteIndex] & mask) >> (8 - horizontalBitOffset - bitsPerPixel);
                        
                        // 写入到垂直数据中
                        int verticalPixelIndex = row; // 在当前列中的位置
                        int verticalByteIndex = verticalPixelIndex / pixelsPerByte;
                        int verticalBitOffset = (verticalPixelIndex % pixelsPerByte) * bitsPerPixel;
                        
                        int targetByteIndex = columnStartByte + verticalByteIndex;
                        if (targetByteIndex < verticalData.Length)
                        {
                            verticalData[targetByteIndex] |= (byte)(pixelValue << (8 - verticalBitOffset - bitsPerPixel));
                        }
                    }
                }
            }

            return verticalData;
        }

        /// <summary>
        /// 反转位序
        /// </summary>
        private byte[]? ReverseBitOrder(byte[]? data)
        {
            if (data == null)
                return null;

            byte[] reversed = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                reversed[i] = ReverseByte(data[i]);
            }

            return reversed;
        }

        /// <summary>
        /// 反转字节中的位序
        /// </summary>
        private byte ReverseByte(byte value)
        {
            byte result = 0;
            for (int i = 0; i < 8; i++)
            {
                result = (byte)((result << 1) | ((value >> i) & 1));
            }
            return result;
        }

        /// <summary>
        /// 获取字符集统计信息（基于实际渲染数据）
        /// </summary>
        /// <param name="fontData">字体点阵数据</param>
        /// <returns>统计信息</returns>
        public FontRenderStatistics GetRenderStatistics(FontBitmapData fontData)
        {
            return fontData.GetStatistics();
        }

        /// <summary>
        /// 获取字符集预估统计信息（渲染前）
        /// </summary>
        /// <param name="charset">字符集</param>
        /// <returns>预估统计信息</returns>
        public FontRenderStatistics GetEstimatedStatistics(CharsetManager charset)
        {
            var stats = new FontRenderStatistics
            {
                TotalCharacters = charset.CharCount,
                FontFamily = FontFamily,
                FontSize = FontSize,
                CanvasWidth = Width,
                CanvasHeight = Height,
                BitsPerPixel = BitsPerPixel
            };

            // 计算预估数据大小
            int pixelsPerByte = 8 / BitsPerPixel;
            int bytesPerChar = ((Width * Height) + pixelsPerByte - 1) / pixelsPerByte;
            
            if (!IsFixedWidth)
            {
                bytesPerChar += 4; // 预估宽高信息各2字节
            }

            stats.TotalDataSize = bytesPerChar * charset.CharCount;
            stats.BytesPerCharacter = bytesPerChar;

            return stats;
        }

        public void Dispose()
        {
            _renderer?.Dispose();
        }
    }

}