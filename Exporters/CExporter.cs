using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FontMaker.Data.Models;

namespace FontMaker.Exporters
{
    /// <summary>
    /// C/C++ 字体文件导出器
    /// </summary>
    public class CExporter
    {
        private string _filePath;

        public CExporter(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// 导出字体数据为C头文件格式
        /// </summary>
        /// <param name="fontData">字体数据</param>
        /// <param name="fontName">字体名称</param>
        /// <returns>是否导出成功</returns>
        public bool Export(FontBitmapData fontData, string fontName)
        {
            try
            {
                var content = GenerateCContent(fontData, fontName);
                File.WriteAllText(_filePath, content, Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 生成C格式的字体内容
        /// </summary>
        private string GenerateCContent(FontBitmapData fontData, string fontName)
        {
            var sb = new StringBuilder();
            var metadata = fontData.Metadata;
            var characters = fontData.Characters;
            int charCount = characters.Count;

            // 文件头注释
            sb.AppendLine("/*******************************************************************************");
            sb.AppendLine($"* {FontMaker.Resources.Lang.Languages.FileGeneratedBy}");
            sb.AppendLine("* GitHub: https://github.com/WangONC/FontMaker");
            sb.AppendLine($"* {FontMaker.Resources.Lang.Languages.Generated}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("*******************************************************************************/");

            // 字体基本参数宏定义
            sb.AppendLine($"#define FONT_MONOSPACE  {(metadata.IsFixedWidth ? 1 : 0)}    // {FontMaker.Resources.Lang.Languages.FontTypeMonospace}");
            sb.AppendLine($"#define FONT_WIDTH      {metadata.MaxCharacterWidth}    // {FontMaker.Resources.Lang.Languages.FontWidth}");
            sb.AppendLine($"#define FONT_HEIGHT     {metadata.MaxCharacterHeight}   // {FontMaker.Resources.Lang.Languages.FontHeight}");
            sb.AppendLine($"#define FONT_BPP        {metadata.BitsPerPixel}    // {FontMaker.Resources.Lang.Languages.FontBPP}");
            sb.AppendLine($"#define FONT_PITCH      {(metadata.MaxCharacterWidth * metadata.BitsPerPixel + 7) / 8}    // {FontMaker.Resources.Lang.Languages.FontPitch}");
            sb.AppendLine($"#define FONT_CHAR_COUNT {charCount}   // {FontMaker.Resources.Lang.Languages.FontCharCount}");
            sb.AppendLine();
            
            // 获取正确的字体显示名称
            string fontDisplayName = GetFontDisplayName(fontName, metadata.FontFamily);

            // 数据格式日志
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.DataFormatDescription}");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.FontFamily}: {fontDisplayName}");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.FontSizePixels}: {metadata.FontSize}px");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.CanvasSize}: {metadata.CanvasWidth}x{metadata.CanvasHeight}");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.ScanMethod}: {(metadata.IsHorizontalScan ? FontMaker.Resources.Lang.Languages.HorizontalScan : FontMaker.Resources.Lang.Languages.VerticalScan)}");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.BitOrder}: {(metadata.IsHighBitFirst ? FontMaker.Resources.Lang.Languages.LeftToRightLabel : FontMaker.Resources.Lang.Languages.RightToLeftLabel)}");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.WidthMode}: {(metadata.IsFixedWidth ? FontMaker.Resources.Lang.Languages.FixedWidthLabel : FontMaker.Resources.Lang.Languages.VariableWidthLabel)}");
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.CharSet}: {metadata.CharsetName}");
            sb.AppendLine();

            // 添加数据结构和读取方法说明
            if (!metadata.IsFixedWidth)
            {
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.VariableWidthDataStructure}");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.WidthBytesCount}: {metadata.WidthBytesCount} ({string.Format(FontMaker.Resources.Lang.Languages.DeterminedByMaxWidth, metadata.MaxCharacterWidth)})");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.HeightBytesCount}: {metadata.HeightBytesCount} ({string.Format(FontMaker.Resources.Lang.Languages.DeterminedByMaxHeight, metadata.MaxCharacterHeight)})");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.CharacterDataFormat}: {string.Format(FontMaker.Resources.Lang.Languages.DataCompositionBreakdown, metadata.WidthBytesCount, metadata.HeightBytesCount)}");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.LittleEndianStorage}");
                sb.AppendLine("//");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.IndexAccessDescription}");
                sb.AppendLine($"// - get_char_info(index): {FontMaker.Resources.Lang.Languages.GetCharInfoFromTable}");
                sb.AppendLine($"// - get_raw_char_data_by_index(index): {FontMaker.Resources.Lang.Languages.VariableWidthRawAccess}");
                sb.AppendLine($"// - get_char_info_by_char(ch): {FontMaker.Resources.Lang.Languages.GetCharInfoByChar}");
                sb.AppendLine();
                
                // 添加读取辅助函数
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.VariableWidthCharReadHelper}");
                sb.AppendLine("typedef struct {");
                sb.AppendLine("    unsigned int width;");
                sb.AppendLine("    unsigned int height;");
                sb.AppendLine("    const unsigned char* data;");
                sb.AppendLine("} char_info_t;");
                sb.AppendLine();
                
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.GetCharInfoFromTable}");
                sb.AppendLine("char_info_t get_char_info(int index) {");
                sb.AppendLine("    char_info_t info = {0};");
                sb.AppendLine("    const unsigned char* ptr = CHAR_BITS[index];");
                sb.AppendLine("    ");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.ReadWidthLittleEndian}");
                GenerateReadBytesCode(sb, "info.width", metadata.WidthBytesCount);
                sb.AppendLine("    ");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.ReadHeightLittleEndian}");
                GenerateReadBytesCode(sb, "info.height", metadata.HeightBytesCount);
                sb.AppendLine("    ");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.BitmapDataStartPosition}");
                sb.AppendLine($"    info.data = ptr;");
                sb.AppendLine("    ");
                sb.AppendLine("    return info;");
                sb.AppendLine("}");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.FixedWidthDataStructure}");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.FixedWidthCharAccess}");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.DirectCharAccess}");
                sb.AppendLine("//");
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.IndexAccessDescription}");
                sb.AppendLine($"// - get_char_data_by_index(index): {FontMaker.Resources.Lang.Languages.IndexAccessByIndex}");
                sb.AppendLine($"// - get_char_data_by_char(ch): {FontMaker.Resources.Lang.Languages.IndexAccessByChar}");
                sb.AppendLine();
            }

            // 字符映射表
            sb.Append("static const unsigned char CHAR_TABLE[] = \"");
            foreach (var charInfo in characters)
            {
                char ch = charInfo.Character;
                // 转换为UTF-8字符串表示
                if (ch == '"') sb.Append("\\\"");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch == '\n') sb.Append("\\n");
                else if (ch == '\r') sb.Append("\\r");
                else if (ch == '\t') sb.Append("\\t");
                else if (ch >= 32 && ch <= 126) sb.Append(ch);
                else
                {
                    // 对于不可显示字符，使用十六进制转义
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(ch.ToString());
                    foreach (byte b in utf8Bytes)
                    {
                        sb.Append($"\\x{b:X2}");
                    }
                }
            }
            sb.AppendLine("\";");
            sb.AppendLine();

            if (metadata.IsFixedWidth)
            {
                // 固定宽度模式：使用二维数组
                int bytesPerChar = CalculateBytesPerChar(metadata);
                sb.AppendLine($"static const unsigned char CHAR_BITS[FONT_CHAR_COUNT][{bytesPerChar}] =");
                sb.AppendLine("{");

                for (int i = 0; i < charCount; i++)
                {
                    var charInfo = characters[i];
                    char ch = charInfo.Character;
                    byte[] pixelData = GetCharacterBytes(charInfo, bytesPerChar);

                    // 字符注释
                    string charDesc = GetCharacterDescription(ch);
                    sb.AppendLine($"    // U+{(int)ch:X4}({charDesc})");

                    // 生成十六进制数据行
                    sb.Append("    { ");
                    for (int j = 0; j < bytesPerChar; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append(", ");
                            // 每16字节换行对齐
                            if (j % 16 == 0)
                            {
                                sb.AppendLine();
                                sb.Append("      ");
                            }
                        }
                        sb.Append($"0x{pixelData[j]:X2}");
                    }

                    if (i < charCount - 1)
                        sb.AppendLine(" },");
                    else
                        sb.AppendLine(" }");
                }

                sb.AppendLine("};");
            }
            else
            {
                // 可变宽度模式：生成单独的数据数组和指针数组
                
                // 首先生成所有字符的数据数组
                var charDataArrays = new List<string>();
                for (int i = 0; i < charCount; i++)
                {
                    var charInfo = characters[i];
                    char ch = charInfo.Character;
                    byte[] pixelData = charInfo.GetCompleteData(); // 包含宽高信息的完整数据

                    string charDesc = GetCharacterDescription(ch);
                    string arrayName = $"char_data_{i}";
                    charDataArrays.Add(arrayName);
                    
                    sb.AppendLine($"// U+{(int)ch:X4}({charDesc}) - {charInfo.ActualWidth}x{charInfo.ActualHeight}");
                    sb.Append($"static const unsigned char {arrayName}[] = {{ ");
                    
                    for (int j = 0; j < pixelData.Length; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append(", ");
                            // 每16字节换行对齐
                            if (j % 16 == 0)
                            {
                                sb.AppendLine();
                                sb.Append("    ");
                            }
                        }
                        sb.Append($"0x{pixelData[j]:X2}");
                    }
                    sb.AppendLine(" };");
                }
                
                sb.AppendLine();
                
                // 生成指针数组
                sb.AppendLine("static const unsigned char* CHAR_BITS[FONT_CHAR_COUNT] =");
                sb.AppendLine("{");
                for (int i = 0; i < charCount; i++)
                {
                    sb.Append($"    {charDataArrays[i]}");
                    if (i < charCount - 1)
                        sb.AppendLine(",");
                    else
                        sb.AppendLine();
                }
                sb.AppendLine("};");
            }
            sb.AppendLine();

            // 添加索引访问函数
            GenerateIndexFunctions(sb, metadata, charCount);

            return sb.ToString();
        }

        /// <summary>
        /// 计算每个字符需要的字节数
        /// </summary>
        private int CalculateBytesPerChar(FontMetadata metadata)
        {
            if (metadata.IsFixedWidth)
            {
                // 固定宽度模式：仅包含点阵数据
                int totalBits = metadata.MaxCharacterWidth * metadata.MaxCharacterHeight * metadata.BitsPerPixel;
                return (totalBits + 7) / 8; // 向上取整到字节边界
            }
            else
            {
                // 可变宽度模式：最大可能的字节数（包含宽高信息）
                int maxBitmapBits = metadata.MaxCharacterWidth * metadata.MaxCharacterHeight * metadata.BitsPerPixel;
                int maxBitmapBytes = (maxBitmapBits + 7) / 8;
                return metadata.WidthBytesCount + metadata.HeightBytesCount + maxBitmapBytes;
            }
        }

        /// <summary>
        /// 获取字符的字节数据，不足的部分用0填充
        /// </summary>
        private byte[] GetCharacterBytes(CharacterBitmapResult charInfo, int requiredBytes)
        {
            byte[] result = new byte[requiredBytes];
            
            if (charInfo.WidthBytes.Length > 0 && charInfo.HeightBytes.Length > 0)
            {
                // 可变宽度模式：包含宽高信息 + 点阵数据
                byte[] completeData = charInfo.GetCompleteData();
                int copyLength = Math.Min(completeData.Length, requiredBytes);
                Array.Copy(completeData, 0, result, 0, copyLength);
            }
            else
            {
                // 固定宽度模式：仅包含点阵数据
                byte[] sourceData = charInfo.GetBitmapBytes();
                int copyLength = Math.Min(sourceData.Length, requiredBytes);
                Array.Copy(sourceData, 0, result, 0, copyLength);
            }
            
            return result;
        }

        /// <summary>
        /// 生成读取多字节数据的C代码
        /// </summary>
        private void GenerateReadBytesCode(StringBuilder sb, string varName, int byteCount)
        {
            sb.Append($"    {varName} = ");
            for (int i = 0; i < byteCount; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append($"(ptr[{i}] << {i * 8})");
            }
            sb.AppendLine(";");
            sb.AppendLine($"    ptr += {byteCount};");
        }

        /// <summary>
        /// 生成索引访问函数
        /// </summary>
        private void GenerateIndexFunctions(StringBuilder sb, FontMetadata metadata, int charCount)
        {
            // 通用函数：根据字符获取索引
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.GetCharIndexByChar}");
            sb.AppendLine("int get_char_index(unsigned char ch) {");
            sb.AppendLine("    for (int i = 0; i < FONT_CHAR_COUNT; i++) {");
            sb.AppendLine("        if ((unsigned char)CHAR_TABLE[i] == ch) {");
            sb.AppendLine("            return i;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine($"    return -1; // {FontMaker.Resources.Lang.Languages.CharNotFound}");
            sb.AppendLine("}");
            sb.AppendLine();

            // 根据索引获取字符数据
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.IndexAccessByIndex}");
            sb.AppendLine("const unsigned char* get_char_data_by_index(int index) {");
            sb.AppendLine("    if (index < 0 || index >= FONT_CHAR_COUNT) {");
            sb.AppendLine("        return NULL;");
            sb.AppendLine("    }");
            if (metadata.IsFixedWidth)
            {
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FixedWidthDirectAccess}");
                sb.AppendLine("    return CHAR_BITS[index];");
            }
            else
            {
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.VariableWidthRawAccess}");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.UseCharInfoForParsing}");
                sb.AppendLine("    return CHAR_BITS[index];");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            // 统一函数原型：根据字符获取字符数据
            sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.IndexAccessByChar}");
            sb.AppendLine("const unsigned char* get_char_data_by_char(unsigned char ch) {");
            sb.AppendLine("    int index = get_char_index(ch);");
            sb.AppendLine("    if (index < 0) {");
            sb.AppendLine("        return NULL;");
            sb.AppendLine("    }");
            sb.AppendLine("    return get_char_data_by_index(index);");
            sb.AppendLine("}");
            sb.AppendLine();

            // 可变宽度字体额外提供解析函数
            if (!metadata.IsFixedWidth)
            {
                sb.AppendLine($"// {FontMaker.Resources.Lang.Languages.VariableWidthExclusiveAccess}");
                sb.AppendLine("char_info_t get_char_info_by_char(unsigned char ch) {");
                sb.AppendLine("    int index = get_char_index(ch);");
                sb.AppendLine("    if (index < 0) {");
                sb.AppendLine("        char_info_t empty = {0};");
                sb.AppendLine("        return empty;");
                sb.AppendLine("    }");
                sb.AppendLine("    return get_char_info(index);");
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// 获取正确的字体显示名称
        /// </summary>
        private string GetFontDisplayName(string fontName, string metadataFontFamily)
        {
            try
            {
                // 如果fontName包含路径信息，尝试解析FontFamily并使用转换器
                if (fontName.Contains("file:///") || fontName.Contains("#"))
                {
                    var fontFamily = new System.Windows.Media.FontFamily(metadataFontFamily);
                    var converter = new FontMaker.Converters.FontDisplayNameConverter();
                    var displayName = converter.Convert(fontFamily, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
                    if (displayName is string name && !string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                
                // 使用传入的fontName
                return string.IsNullOrEmpty(fontName) ? metadataFontFamily : fontName;
            }
            catch (Exception)
            {
                // 转换失败时回退到原始名称
                return string.IsNullOrEmpty(fontName) ? metadataFontFamily : fontName;
            }
        }

        /// <summary>
        /// 获取字符的描述文本，用于注释
        /// </summary>
        private string GetCharacterDescription(char ch)
        {
            if (ch >= 32 && ch <= 126)
            {
                // 可显示ASCII字符
                return ch switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    _ => ch.ToString()
                };
            }
            else
            {
                // 控制字符或扩展字符
                return ch switch
                {
                    '\0' => "NULL",
                    '\n' => "LF",
                    '\r' => "CR",
                    '\t' => "TAB",
                    ' ' => "SPACE",
                    _ => $"0x{(int)ch:X2}"
                };
            }
        }
    }
}