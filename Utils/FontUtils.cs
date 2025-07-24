using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using FontStyle = System.Windows.FontStyle;

namespace FontMaker.Utils
{
    /// <summary>
    /// 支持完整Unicode平面的字体工具类，但是受到当前项目架构限制，估计只会使用0x0000到0xFFFF范围内的字符。
    /// </summary>
    class FontUtils
    {
        public System.Windows.Media.FontFamily fontFamily { get; private set; }
        public FontStyle Style { get; private set; }
        public FontWeight Weight { get; private set; }
        public FontStretch Stretch { get; private set; }
        public Typeface Typeface { get; private set; }

        FontUtils(System.Windows.Media.FontFamily fontFamily,FontStyle style, FontWeight weight, FontStretch stretch)
        {
            this.fontFamily = fontFamily;
            this.Style = style;
            this.Weight = weight;
            this.Stretch = stretch;
            this.Typeface = new Typeface(fontFamily, style, weight, stretch);
        }

        public FontUtils(Typeface typeface)
        {
            this.Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
            this.fontFamily = typeface.FontFamily;
            this.Style = typeface.Style;
            this.Weight = typeface.Weight;
            this.Stretch = typeface.Stretch;
        }
        public List<int> GetSupportedUnicodeCodePoints()
        {
            var supportedCodePoints = new List<int>();

            if (Typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                foreach (int unicodeValue in glyphTypeface.CharacterToGlyphMap.Keys)
                {
                    // 确保Unicode值在有效范围内
                    if (unicodeValue >= 0 && unicodeValue <= 0x10FFFF)
                    {
                        supportedCodePoints.Add(unicodeValue);
                    }
                }
            }

            return supportedCodePoints.OrderBy(c => c).ToList();
        }

        public List<char> GetSupportedBMPUnicodeCodePoints()
        {
            var supportedCodePoints = new List<char>();

            if (Typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                foreach (char unicodeValue in glyphTypeface.CharacterToGlyphMap.Keys)
                {
                    // 确保Unicode值在BMP范围内
                    if (unicodeValue >= 0 && unicodeValue <= 0xFFFF)
                    {
                        supportedCodePoints.Add(unicodeValue);
                    }
                }
            }
            return supportedCodePoints.OrderBy(c => c).ToList();

        }

        /// <summary>
        /// 获取指定Unicode范围内的支持字符
        /// </summary>
        public List<int> GetSupportedCodePointsInRange(int startUnicode, int endUnicode)
        {
            var allSupportedCodePoints = GetSupportedUnicodeCodePoints();
            return allSupportedCodePoints.Where(c => c >= startUnicode && c <= endUnicode).ToList();
        }

        public List<char> GetSupportedBMPCodePointsInRange(int startUnicode, int endUnicode)
        {
            var allSupportedCodePoints = GetSupportedBMPUnicodeCodePoints();
            return allSupportedCodePoints.Where(c => c >= startUnicode && c <= endUnicode).ToList();
        }

        /// <summary>
        /// 检查字体是否支持指定字符
        /// </summary>
        public bool SupportsCharacter(char character)
        {
            if (Typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                return glyphTypeface.CharacterToGlyphMap.ContainsKey(character);
            }
            return false;
        }

        /// <summary>
        /// 获取字体支持的ASCII字符
        /// </summary>
        public List<char> GetSupportedAsciiCharacters()
        {
            return GetSupportedBMPCodePointsInRange(32, 126); // 可打印ASCII字符
        }

        // <summary>
        /// 获取字体的字形数量
        /// </summary>
        public int GetGlyphCount()
        {
            if (Typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                return glyphTypeface.GlyphCount;
            }
            return 0;
        }

        public string GetFaceName(string languageCode = null)
        {
            if (Typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                // 确定目标语言
                XmlLanguage targetLanguage;
                if (string.IsNullOrEmpty(languageCode))
                {
                    // 使用当前UI语言
                    targetLanguage = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
                }
                else
                {
                    targetLanguage = XmlLanguage.GetLanguage(languageCode);
                }

                foreach (var kvp in glyphTypeface.FaceNames)
                {
                    if (kvp.Key.IetfLanguageTag.Equals(targetLanguage.IetfLanguageTag, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
            }
            return string.Empty;
        }

        public string GetFamilyName(string languageCode = null)
        {
            if (Typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                // 确定目标语言
                XmlLanguage targetLanguage;
                if (string.IsNullOrEmpty(languageCode))
                {
                    // 使用当前UI语言
                    targetLanguage = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
                }
                else
                {
                    targetLanguage = XmlLanguage.GetLanguage(languageCode);
                }

                foreach (var kvp in glyphTypeface.FamilyNames)
                {
                    if (kvp.Key.IetfLanguageTag.Equals(targetLanguage.IetfLanguageTag, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
            }
            return string.Empty;
        }



    }
}
