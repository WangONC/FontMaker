using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace FontMaker.Data.Models
{
    /// <summary>
    /// 字体点阵数据模型 - 存储完整的字体渲染信息和二进制数据
    /// </summary>
    public class FontBitmapData
    {
        /// <summary>
        /// 字体基本信息
        /// </summary>
        public FontMetadata Metadata { get; set; } = new FontMetadata();

        /// <summary>
        /// 所有字符的渲染结果
        /// </summary>
        public List<CharacterBitmapResult> Characters { get; set; } = new List<CharacterBitmapResult>();

        /// <summary>
        /// 完整的二进制数据流（用于导出）
        /// 结构：[头信息][字符数据流]
        /// </summary>
        public BitArray CompleteBitStream { get; set; } = new BitArray(0);

        /// <summary>
        /// 获取字符集统计信息
        /// </summary>
        public FontRenderStatistics GetStatistics()
        {
            return new FontRenderStatistics
            {
                TotalCharacters = Characters.Count,
                FontFamily = Metadata.FontFamily,
                FontSize = Metadata.FontSize,
                CanvasWidth = Metadata.CanvasWidth,
                CanvasHeight = Metadata.CanvasHeight,
                BitsPerPixel = Metadata.BitsPerPixel,
                MaxCharacterWidth = Metadata.MaxCharacterWidth,
                MaxCharacterHeight = Metadata.MaxCharacterHeight,
                WidthBytesCount = Metadata.WidthBytesCount,
                HeightBytesCount = Metadata.HeightBytesCount,
                TotalDataSize = CompleteBitStream.Length / 8,
                BytesPerCharacter = GetAverageBytesPerCharacter()
            };
        }

        /// <summary>
        /// 获取完整的字节数组（用于文件导出）
        /// </summary>
        public byte[] GetCompleteByteArray()
        {
            byte[] bytes = new byte[(CompleteBitStream.Length + 7) / 8];
            CompleteBitStream.CopyTo(bytes, 0);
            return bytes;
        }

        /// <summary>
        /// 获取指定字符的数据偏移量和长度
        /// 返回 (offset, length) 用于快速定位字符数据
        /// </summary>
        public (int offset, int length) GetCharacterDataPosition(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex >= Characters.Count)
                throw new ArgumentOutOfRangeException(nameof(characterIndex));

            int offset = 0;
            
            for (int i = 0; i < characterIndex; i++)
            {
                var character = Characters[i];
                
                // 可变宽度模式需要加上宽高信息的长度
                if (!Metadata.IsFixedWidth)
                {
                    offset += (Metadata.WidthBytesCount + Metadata.HeightBytesCount) * 8;
                }
                
                // 加上字符数据长度
                offset += character.BitmapData.Length;
            }

            // 当前字符的长度
            int length = Characters[characterIndex].BitmapData.Length;
            if (!Metadata.IsFixedWidth)
            {
                length += (Metadata.WidthBytesCount + Metadata.HeightBytesCount) * 8;
            }

            return (offset, length);
        }


        private int GetAverageBytesPerCharacter()
        {
            if (Characters.Count == 0) return 0;
            
            long totalBits = 0;
            foreach (var character in Characters)
            {
                totalBits += character.BitmapData.Length;
            }
            
            return (int)((totalBits / Characters.Count + 7) / 8); // 向上取整到字节
        }
    }

    /// <summary>
    /// 字体元数据信息
    /// </summary>
    public class FontMetadata
    {
        /// <summary>
        /// 字体名称
        /// </summary>
        public string FontFamily { get; set; } = "";

        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// 画布宽度
        /// </summary>
        public int CanvasWidth { get; set; }

        /// <summary>
        /// 画布高度
        /// </summary>
        public int CanvasHeight { get; set; }

        /// <summary>
        /// 每像素位数 (1, 2, 4, 8)
        /// </summary>
        public int BitsPerPixel { get; set; } = 1;

        /// <summary>
        /// 最大字符宽度（用于计算宽度字节数）
        /// </summary>
        public int MaxCharacterWidth { get; set; }

        /// <summary>
        /// 最大字符高度（用于计算高度字节数）
        /// </summary>
        public int MaxCharacterHeight { get; set; }

        /// <summary>
        /// 宽度信息需要的字节数 (1-4字节)
        /// </summary>
        public int WidthBytesCount { get; set; }

        /// <summary>
        /// 高度信息需要的字节数 (1-4字节)
        /// </summary>
        public int HeightBytesCount { get; set; }

        /// <summary>
        /// 是否水平扫描
        /// </summary>
        public bool IsHorizontalScan { get; set; } = true;

        /// <summary>
        /// 是否高位在前 (MSB)
        /// </summary>
        public bool IsHighBitFirst { get; set; } = true;

        /// <summary>
        /// 是否固定宽度
        /// </summary>
        public bool IsFixedWidth { get; set; } = true;

        /// <summary>
        /// 字符集名称
        /// </summary>
        public string CharsetName { get; set; } = "";

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 单个字符的点阵结果
    /// </summary>
    public class CharacterBitmapResult
    {
        /// <summary>
        /// 字符
        /// </summary>
        public char Character { get; set; }

        /// <summary>
        /// Unicode编码
        /// </summary>
        public int UnicodeValue => (int)Character;

        /// <summary>
        /// 实际宽度（去除空白后）
        /// </summary>
        public int ActualWidth { get; set; }

        /// <summary>
        /// 实际高度（去除空白后）
        /// </summary>
        public int ActualHeight { get; set; }

        /// <summary>
        /// 原始画布宽度
        /// </summary>
        public int CanvasWidth { get; set; }

        /// <summary>
        /// 原始画布高度
        /// </summary>
        public int CanvasHeight { get; set; }

        /// <summary>
        /// 点阵数据（使用BitArray存储大数据）
        /// </summary>
        public BitArray BitmapData { get; set; } = new BitArray(0);

        /// <summary>
        /// 宽度信息字节（可变宽度模式下使用）
        /// </summary>
        public byte[] WidthBytes { get; set; } = new byte[0];

        /// <summary>
        /// 高度信息字节（可变宽度模式下使用）
        /// </summary>
        public byte[] HeightBytes { get; set; } = new byte[0];

        /// <summary>
        /// 获取完整的字符数据（包含宽高信息 + 点阵数据）
        /// </summary>
        public byte[] GetCompleteData()
        {
            var result = new List<byte>();
            
            // 添加宽度信息
            result.AddRange(WidthBytes);
            
            // 添加高度信息
            result.AddRange(HeightBytes);
            
            // 添加点阵数据
            byte[] bitmapBytes = new byte[(BitmapData.Length + 7) / 8];
            BitmapData.CopyTo(bitmapBytes, 0);
            result.AddRange(bitmapBytes);
            
            return result.ToArray();
        }

        /// <summary>
        /// 获取点阵数据的字节表示
        /// </summary>
        public byte[] GetBitmapBytes()
        {
            byte[] bytes = new byte[(BitmapData.Length + 7) / 8];
            BitmapData.CopyTo(bytes, 0);
            return bytes;
        }
    }

    /// <summary>
    /// 扩展的字体渲染统计信息
    /// </summary>
    public class FontRenderStatistics
    {
        public int TotalCharacters { get; set; }
        public string FontFamily { get; set; } = "";
        public int FontSize { get; set; }
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }
        public int BitsPerPixel { get; set; }
        public int MaxCharacterWidth { get; set; }
        public int MaxCharacterHeight { get; set; }
        public int WidthBytesCount { get; set; }
        public int HeightBytesCount { get; set; }
        public int TotalDataSize { get; set; }
        public int BytesPerCharacter { get; set; }
    }

}