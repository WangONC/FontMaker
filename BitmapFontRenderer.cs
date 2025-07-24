using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using FontMaker.Data.Models;

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
        public bool IsHighBitFirst { get; set; } = true;
        public bool IsFixedWidth { get; set; } = true;

        public BitmapFontRenderer(int width, int height, string fontFamily, int fontSize, System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular)
        {
            Width = width;
            Height = height;
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontStyle = fontStyle;

            _renderer = new CharacterRenderer(width, height, fontFamily, fontSize, fontStyle);
        }

        /// <summary>
        /// 渲染单个字符获取预览图像
        /// </summary>
        /// <param name="character">要渲染的字符</param>
        /// <returns>预览图像</returns>
        public BitmapSource RenderCharacterPreview(char character)
        {
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

            var previewImage = _renderer.RenderCharacter(character);
            var pixelData = _renderer.GetCharacterPixelData(character, BitsPerPixel);
            var actualWidth = _renderer.GetCharacterWidth(character);

            // 根据扫描方式重新组织数据
            if (!IsHorizontalScan)
            {
                pixelData = ConvertToVerticalScan(pixelData);
            }

            // 根据位序要求调整
            if (!IsHighBitFirst)
            {
                pixelData = ReverseBitOrder(pixelData);
            }

            return new CharacterRenderResult
            {
                PreviewImage = previewImage,
                PixelData = pixelData,
                ActualWidth = actualWidth,
                Character = character
            };
        }

        /// <summary>
        /// 批量渲染字符集
        /// </summary>
        /// <param name="charset">字符集</param>
        /// <returns>渲染结果列表</returns>
        public List<CharacterRenderResult> RenderCharset(CharsetManager charset)
        {
            var results = new List<CharacterRenderResult>();

            foreach (char character in charset.Characters)
            {
                try
                {
                    var result = RenderCharacter(character);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    // 记录渲染失败的字符，但继续处理其他字符
                    System.Diagnostics.Debug.WriteLine($"Failed to render character '{character}': {ex.Message}");
                }
            }

            return results;
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

            _renderer.UpdateParameters(width, height, fontFamily, fontSize, fontStyle);
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
        private byte[] ConvertToVerticalScan(byte[] horizontalData)
        {
            // TODO: 实现水平扫描到垂直扫描的转换
            // 这需要根据具体的位深度和像素排列进行实现
            return horizontalData; // 临时返回，需要具体实现
        }

        /// <summary>
        /// 反转位序
        /// </summary>
        private byte[] ReverseBitOrder(byte[] data)
        {
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
        /// 获取字符集统计信息
        /// </summary>
        /// <param name="charset">字符集</param>
        /// <returns>统计信息</returns>
        public FontRenderStatistics GetRenderStatistics(CharsetManager charset)
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

            // 计算数据大小
            int pixelsPerByte = 8 / BitsPerPixel;
            int bytesPerChar = ((Width * Height) + pixelsPerByte - 1) / pixelsPerByte;
            
            if (!IsFixedWidth)
            {
                bytesPerChar += 1; // 宽度信息
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

    /// <summary>
    /// 字体渲染统计信息
    /// </summary>
    public class FontRenderStatistics
    {
        public int TotalCharacters { get; set; }
        public string FontFamily { get; set; } = "";
        public int FontSize { get; set; }
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }
        public int BitsPerPixel { get; set; }
        public int TotalDataSize { get; set; }
        public int BytesPerCharacter { get; set; }
    }
}