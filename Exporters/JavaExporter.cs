using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FontMaker.Data.Models;

namespace FontMaker.Exporters
{
    /// <summary>
    /// Java 字体文件导出器
    /// </summary>
    public class JavaExporter
    {
        private string _filePath;

        public JavaExporter(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// 导出字体数据为Java源代码格式
        /// </summary>
        /// <param name="fontData">字体数据</param>
        /// <param name="fontName">字体名称</param>
        /// <returns>是否导出成功</returns>
        public bool Export(FontBitmapData fontData, string fontName)
        {
            try
            {
                var content = GenerateJavaContent(fontData, fontName);
                File.WriteAllText(_filePath, content, Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 生成Java格式的字体内容
        /// </summary>
        private string GenerateJavaContent(FontBitmapData fontData, string fontName)
        {
            var sb = new StringBuilder();
            var metadata = fontData.Metadata;
            var characters = fontData.Characters;
            int charCount = characters.Count;

            // 获取正确的字体显示名称
            string fontDisplayName = GetFontDisplayName(fontName, metadata.FontFamily);

            // 获取类名（从文件路径中提取，去除扩展名）
            string className = Path.GetFileNameWithoutExtension(_filePath);
            if (string.IsNullOrEmpty(className) || !char.IsLetter(className[0]))
            {
                className = "BitmapFont";
            }

            // 文件头注释
            sb.AppendLine("/**");
            sb.AppendLine($" * {FontMaker.Resources.Lang.Languages.FileGeneratedBy}");
            sb.AppendLine(" * GitHub: https://github.com/WangONC/FontMaker");
            sb.AppendLine($" * {FontMaker.Resources.Lang.Languages.Generated}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(" */");
            sb.AppendLine();

            // 类声明
            sb.AppendLine($"public class {className} {{");
            sb.AppendLine();

            // 字体基本参数常量
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontTypeMonospace}");
            sb.AppendLine($"    public static final boolean FONT_MONOSPACE = {(metadata.IsFixedWidth ? "true" : "false")};");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontWidth}");
            sb.AppendLine($"    public static final int FONT_WIDTH = {metadata.MaxCharacterWidth};");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontHeight}");
            sb.AppendLine($"    public static final int FONT_HEIGHT = {metadata.MaxCharacterHeight};");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontBPP}");
            sb.AppendLine($"    public static final int FONT_BPP = {metadata.BitsPerPixel};");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontPitch}");
            sb.AppendLine($"    public static final int FONT_PITCH = {(metadata.MaxCharacterWidth * metadata.BitsPerPixel + 7) / 8};");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontCharCount}");
            sb.AppendLine($"    public static final int FONT_CHAR_COUNT = {charCount};");
            sb.AppendLine();

            // 数据格式说明
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.DataFormatDescription}");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontFamily}: {fontDisplayName}");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FontSizePixels}: {metadata.FontSize}px");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.CanvasSize}: {metadata.CanvasWidth}x{metadata.CanvasHeight}");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.ScanMethod}: {(metadata.IsHorizontalScan ? FontMaker.Resources.Lang.Languages.ScanMethodHorizontal : FontMaker.Resources.Lang.Languages.ScanMethodVertical)}");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.BitOrder}: {(metadata.IsHighBitFirst ? FontMaker.Resources.Lang.Languages.BitOrderLeftToRight : FontMaker.Resources.Lang.Languages.BitOrderRightToLeft)}");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.WidthMode}: {(metadata.IsFixedWidth ? FontMaker.Resources.Lang.Languages.WidthModeFixed : FontMaker.Resources.Lang.Languages.WidthModeVariable)}");
            sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.CharSet}: {metadata.CharsetName}");
            sb.AppendLine();

            // 添加数据结构和读取方法说明
            if (!metadata.IsFixedWidth)
            {
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.VariableWidthDataStructure}");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.WidthBytesCount}: {metadata.WidthBytesCount} ({string.Format(FontMaker.Resources.Lang.Languages.DeterminedByMaxWidth, metadata.MaxCharacterWidth)})");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.HeightBytesCount}: {metadata.HeightBytesCount} ({string.Format(FontMaker.Resources.Lang.Languages.DeterminedByMaxHeight, metadata.MaxCharacterHeight)})");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.CharacterDataFormat}: {string.Format(FontMaker.Resources.Lang.Languages.DataCompositionBreakdown, metadata.WidthBytesCount, metadata.HeightBytesCount)}");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.LittleEndianStorage}");
                sb.AppendLine("    //");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.IndexAccessDescription}");
                sb.AppendLine($"    // - get_char_info(index): {FontMaker.Resources.Lang.Languages.GetCharInfoFromTable}");
                sb.AppendLine($"    // - get_raw_char_data_by_index(index): { FontMaker.Resources.Lang.Languages.VariableWidthRawAccess}");
                sb.AppendLine($"    // - get_char_info_by_char(ch): {FontMaker.Resources.Lang.Languages.GetCharInfoByChar}");
                sb.AppendLine();

                // 添加字符信息类
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.VariableWidthCharInfoClass}");
                sb.AppendLine("    public static class CharInfo {");
                sb.AppendLine("        public int width;");
                sb.AppendLine("        public int height;");
                sb.AppendLine("        public byte[] data;");
                sb.AppendLine();
                sb.AppendLine("        public CharInfo(int width, int height, byte[] data) {");
                sb.AppendLine("            this.width = width;");
                sb.AppendLine("            this.height = height;");
                sb.AppendLine("            this.data = data;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();

                // 添加读取辅助方法
                sb.AppendLine($"   // {FontMaker.Resources.Lang.Languages.GetCharInfoFromTable}");
                sb.AppendLine("    public static CharInfo get_char_info(int index) {");
                sb.AppendLine("        if (index < 0 || index >= FONT_CHAR_COUNT) {");
                sb.AppendLine("            return null;");
                sb.AppendLine("        }");
                sb.AppendLine("        ");
                sb.AppendLine("        byte[] charData = CHAR_BITS[index];");
                sb.AppendLine("        int ptr = 0;");
                sb.AppendLine("        ");
                sb.AppendLine($"        // {FontMaker.Resources.Lang.Languages.ReadWidthLittleEndian}");
                GenerateJavaReadBytesCode(sb, "width", metadata.WidthBytesCount);
                sb.AppendLine("        ");
                sb.AppendLine($"        // {FontMaker.Resources.Lang.Languages.ReadHeightLittleEndian}");
                GenerateJavaReadBytesCode(sb, "height", metadata.HeightBytesCount);
                sb.AppendLine("        ");
                sb.AppendLine($"        // {FontMaker.Resources.Lang.Languages.ExtractBitmapData}");
                sb.AppendLine($"        int dataLength = charData.length - {metadata.WidthBytesCount + metadata.HeightBytesCount};");
                sb.AppendLine("        byte[] bitmapData = new byte[dataLength];");
                sb.AppendLine($"        System.arraycopy(charData, {metadata.WidthBytesCount + metadata.HeightBytesCount}, bitmapData, 0, dataLength);");
                sb.AppendLine("        ");
                sb.AppendLine("        return new CharInfo(width, height, bitmapData);");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FixedWidthDataStructure}");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.FixedWidthCharAccess}");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.DirectCharAccess}");
                sb.AppendLine("    //");
                sb.AppendLine($"    // {FontMaker.Resources.Lang.Languages.IndexAccessDescription}");
                sb.AppendLine($"    // - get_char_data_by_index(index): {FontMaker.Resources.Lang.Languages.IndexAccessByIndex}");
                sb.AppendLine($"    // - get_char_data_by_char(ch): {FontMaker.Resources.Lang.Languages.IndexAccessByChar}");
                sb.AppendLine();
            }

            // 字符映射表
            sb.Append("    private static final String CHAR_TABLE = \"");
            foreach (var charInfo in characters)
            {
                char ch = charInfo.Character;
                // 转换为Java字符串表示
                if (ch == '"') sb.Append("\\\"");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch == '\n') sb.Append("\\n");
                else if (ch == '\r') sb.Append("\\r");
                else if (ch == '\t') sb.Append("\\t");
                else if (ch >= 32 && ch <= 126) sb.Append(ch);
                else
                {
                    // 对于不可显示字符，使用Unicode转义
                    sb.Append($"\\u{(int)ch:X4}");
                }
            }
            sb.AppendLine("\";");
            sb.AppendLine();

            // 计算每个字符的字节数
            int bytesPerChar = CalculateBytesPerChar(metadata);

            // 字符点阵数据二维数组
            sb.AppendLine($"    private static final byte[][] CHAR_BITS = {{");

            for (int i = 0; i < charCount; i++)
            {
                var charInfo = characters[i];
                char ch = charInfo.Character;
                byte[] pixelData = GetCharacterBytes(charInfo, bytesPerChar);

                // 字符注释
                string charDesc = GetCharacterDescription(ch);
                sb.AppendLine($"        // U+{(int)ch:X4}({charDesc})");

                // 生成字节数组
                sb.Append("        { ");
                for (int j = 0; j < bytesPerChar; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(", ");
                        // 每16字节换行对齐
                        if (j % 16 == 0)
                        {
                            sb.AppendLine();
                            sb.Append("          ");
                        }
                    }
                    sb.Append($"(byte)0x{pixelData[j]:X2}");
                }

                if (i < charCount - 1)
                    sb.AppendLine(" },");
                else
                    sb.AppendLine(" }");
            }

            sb.AppendLine("    };");
            sb.AppendLine();

            // 获取字符数据的方法
            sb.AppendLine("    public static byte[] get_char_data_by_index(int index) {");
            sb.AppendLine("        if (index < 0 || index >= FONT_CHAR_COUNT) {");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine("        return CHAR_BITS[index].clone();");
            sb.AppendLine("    }");
            sb.AppendLine();

            // 添加索引访问函数
            GenerateJavaIndexFunctions(sb, metadata);

            // 类结束
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成Java索引访问函数
        /// </summary>
        private void GenerateJavaIndexFunctions(StringBuilder sb, FontMetadata metadata)
        {
            // 通用函数：根据字符获取索引
            sb.AppendLine($"   // {FontMaker.Resources.Lang.Languages.GetCharIndexByChar}");
            sb.AppendLine("    public static int get_char_index(char ch) {");
            sb.AppendLine("        String table = CHAR_TABLE;");
            sb.AppendLine("        return table.indexOf(ch);");
            sb.AppendLine("    }");
            sb.AppendLine();

            // 统一函数原型：根据字符获取字符数据
            sb.AppendLine($"   // {FontMaker.Resources.Lang.Languages.IndexAccessByChar}");
            sb.AppendLine("    public static byte[] get_char_data_by_char(char ch) {");
            sb.AppendLine("        int index = get_char_index(ch);");
            sb.AppendLine("        if (index < 0) {");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            if (metadata.IsFixedWidth)
            {
                sb.AppendLine($"       // {FontMaker.Resources.Lang.Languages.FixedWidthDirectAccess}");
                sb.AppendLine("        return get_char_data_by_index(index);");
            }
            else
            {
                sb.AppendLine($"       // {FontMaker.Resources.Lang.Languages.VariableWidthRawAccess}");
                sb.AppendLine($"       // {FontMaker.Resources.Lang.Languages.UseCharInfoForParsing}");
                sb.AppendLine("        return get_char_data_by_index(index);");
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // 可变宽度字体额外提供解析函数
            if (!metadata.IsFixedWidth)
            {
                sb.AppendLine($"   // {FontMaker.Resources.Lang.Languages.VariableWidthExclusiveAccess}");
                sb.AppendLine("    public static CharInfo get_char_info_by_char(char ch) {");
                sb.AppendLine("        int index = get_char_index(ch);");
                sb.AppendLine("        if (index < 0) {");
                sb.AppendLine("            return null;");
                sb.AppendLine("        }");
                sb.AppendLine("        return get_char_info(index);");
                sb.AppendLine("    }");
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
        /// 计算每个字符需要的字节数
        /// </summary>
        private int CalculateBytesPerChar(FontMetadata metadata)
        {
            int totalBits = metadata.MaxCharacterWidth * metadata.MaxCharacterHeight * metadata.BitsPerPixel;
            return (totalBits + 7) / 8; // 向上取整到字节边界
        }

        /// <summary>
        /// 获取字符的字节数据，不足的部分用0填充
        /// </summary>
        private byte[] GetCharacterBytes(CharacterBitmapResult charInfo, int requiredBytes)
        {
            byte[] sourceData = charInfo.GetBitmapBytes();
            byte[] result = new byte[requiredBytes];

            int copyLength = Math.Min(sourceData.Length, requiredBytes);
            Array.Copy(sourceData, 0, result, 0, copyLength);

            return result;
        }

        /// <summary>
        /// 生成读取多字节数据的Java代码
        /// </summary>
        private void GenerateJavaReadBytesCode(StringBuilder sb, string varName, int byteCount)
        {
            sb.Append($"        int {varName} = ");
            for (int i = 0; i < byteCount; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append($"((charData[ptr + {i}] & 0xFF) << {i * 8})");
            }
            sb.AppendLine(";");
            sb.AppendLine($"        ptr += {byteCount};");
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