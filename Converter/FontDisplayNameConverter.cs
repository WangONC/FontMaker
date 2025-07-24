using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace FontMaker.Converters
{
    public class FontDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FontFamily fontFamily)
            {
                return GetFontDisplayName(fontFamily);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取字体的显示名称
        /// </summary>
        private string GetFontDisplayName(FontFamily fontFamily)
        {
            try
            {
                // 尝试获取中文名称
                var currentLanguage = System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name);
                if (fontFamily.FamilyNames.TryGetValue(currentLanguage, out string localizedName))
                {
                    return localizedName;
                }

                // 尝试获取中文（中国）名称
                var chineseLanguage = System.Windows.Markup.XmlLanguage.GetLanguage("zh-CN");
                if (fontFamily.FamilyNames.TryGetValue(chineseLanguage, out string chineseName))
                {
                    return chineseName;
                }

                // 尝试获取英文名称
                var englishLanguage = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
                if (fontFamily.FamilyNames.TryGetValue(englishLanguage, out string englishName))
                {
                    return englishName;
                }

                // 获取第一个可用的名称
                if (fontFamily.FamilyNames.Any())
                {
                    return fontFamily.FamilyNames.First().Value;
                }

                // 如果是系统字体，直接使用Source
                var source = fontFamily.Source;
                if (!source.StartsWith("file:///"))
                {
                    return source;
                }

                // 对于本地字体，从路径中提取文件名（不含扩展名）
                var fileName = System.IO.Path.GetFileNameWithoutExtension(source);
                if (!string.IsNullOrEmpty(fileName))
                {
                    return fileName;
                }

                return source;
            }
            catch
            {
                // 发生异常时返回Source
                return fontFamily.Source;
            }
        }
    }
}
