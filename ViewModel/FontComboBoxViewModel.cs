using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;

namespace FontMaker.ViewModels
{
    public partial class FontComboBoxViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<FontFamily>? _systemFonts;

        [ObservableProperty]
        private ObservableCollection<FontFamily>? _importedFonts;

        [ObservableProperty]
        private ObservableCollection<FontFamily>? _allFonts;

        [ObservableProperty]
        private ObservableCollection<TypefaceInfo>? _fontVariants;

        [ObservableProperty]
        private FontFamily? _selectedFont;

        [ObservableProperty]
        private TypefaceInfo? _selectedVariant;

        public FontComboBoxViewModel()
        {
            ImportedFonts = new ObservableCollection<FontFamily>();
            AllFonts = new ObservableCollection<FontFamily>();
            FontVariants = new ObservableCollection<TypefaceInfo>();

            LoadSystemFonts();
        }

        private void LoadSystemFonts()
        {
            // 获取系统所有字体
            var fonts = new ObservableCollection<FontFamily>();

            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                fonts.Add(fontFamily);
            }

            // 按名称排序
            var sortedFonts = fonts.OrderBy(f => f.Source).ToList();
            SystemFonts = new ObservableCollection<FontFamily>(sortedFonts);

            // 添加到所有字体集合
            if(AllFonts == null)
            {
                AllFonts = new ObservableCollection<FontFamily>();
            }
            AllFonts.Clear();
            foreach (var font in SystemFonts)
            {
                AllFonts.Add(font);
            }

            // 设置默认选中第一个字体
            if (SystemFonts.Count > 0)
            {
                SelectedFont = AllFonts[0];
            }
        }

        // 当选中字体变化时，自动更新字体变体
        partial void OnSelectedFontChanged(FontFamily? value)
        {
            UpdateFontVariants();
        }

        private void UpdateFontVariants()
        {
            FontVariants?.Clear();

            if (SelectedFont == null)
                return;

            try
            {
                var variants = GetAllVariants(SelectedFont);
                foreach (var variant in variants)
                {
                    FontVariants?.Add(new TypefaceInfo(variant));
                }

                // 自动选择第一个变体
                if (FontVariants?.Count > 0)
                {
                    SelectedVariant = FontVariants[0];
                }
            }
            catch (Exception ex)
            {
                // 处理字体加载错误
                NotificationUtils.showMessageError("错误",$"加载字体变体失败: {ex.Message}");
            }
        }

        // <summary>
        /// 获取字体的所有变体
        /// </summary>
        public IEnumerable<Typeface> GetAllVariants(FontFamily fontFamily)
        {
            return fontFamily.GetTypefaces()
                .Distinct()
                .OrderBy(t => t.Weight.ToOpenTypeWeight())
                .ThenBy(t => t.Style.ToString())
                .ToList();
        }

        /// <summary>
        /// 从外部添加导入的字体（供后端代码调用）
        /// </summary>
        public void AddImportedFont(FontFamily fontFamily, string? fontPath = null)
        {
            if (fontFamily == null)
                return;

            // 检查是否已经导入
            if (ImportedFonts?.Any(f => f.Source == fontFamily.Source) == true)
            {
                return; // 已存在，不重复添加
            }

            // 添加到导入字体集合
            ImportedFonts?.Add(fontFamily);
            AllFonts?.Add(fontFamily);

            // 自动选中新导入的字体
            SelectedFont = fontFamily;
        }

        /// <summary>
        /// 移除导入的字体
        /// </summary>
        [RelayCommand]
        private void RemoveImportedFont(FontFamily? fontFamily)
        {
            if (fontFamily == null || ImportedFonts?.Contains(fontFamily) != true)
                return;

            ImportedFonts.Remove(fontFamily);
            AllFonts?.Remove(fontFamily);

            // 如果移除的是当前选中的字体，切换到系统字体
            if (SelectedFont == fontFamily)
            {
                SelectedFont = SystemFonts?.FirstOrDefault();
            }
        }

        /// <summary>
        /// 检查字体是否为导入字体
        /// </summary>
        public bool IsImportedFont(FontFamily fontFamily)
        {
            return ImportedFonts?.Contains(fontFamily) == true;
        }

        /// <summary>
        /// 获取当前选中的Typeface对象
        /// </summary>
        public Typeface? GetSelectedTypeface()
        {
            return SelectedVariant?.Typeface;
        }
    }

    /// <summary>
    /// 字体变体信息包装类
    /// </summary>
    public class TypefaceInfo
    {
        public TypefaceInfo(Typeface typeface)
        {
            Typeface = typeface;
        }

        public Typeface Typeface { get; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName
        {
            get
            {
                var style = GetStyleName(Typeface.Style);
                var weight = GetWeightName(Typeface.Weight);
                var stretch = GetStretchName(Typeface.Stretch);

                var parts = new List<string>();

                if (weight != "常规") parts.Add(weight);
                if (style != "常规") parts.Add(style);
                if (stretch != "常规") parts.Add(stretch);

                return parts.Any() ? string.Join(", ", parts) : "常规";
            }
        }

        private string GetWeightName(FontWeight weight)
        {
            var weightValue = weight.ToOpenTypeWeight();
            return weightValue switch
            {
                // 细体
                <= 100 => "极细",
                <= 150 => "半特细",
                <= 200 => "特细",
                <= 250 => "超细",
                <= 300 => "细体",

                // 常规
                350 => "半细",
                400 => "常规",
                450 => "半中等",

                // 中等
                500 => "中等",
                550 => "半粗",

                // 粗体
                600 => "半粗",
                650 => "中粗",
                700 => "粗体",

                // 特粗
                750 => "特粗",
                800 => "特粗",
                850 => "超粗",
                // 极粗
                900 => "极粗",

                _ => weight.ToString()
            };
        }

        private string GetStyleName(FontStyle style)
        {
            if (style == FontStyles.Normal)
                return "常规";
            if (style == FontStyles.Italic)
                return "斜体";
            if (style == FontStyles.Oblique)
                return "倾斜体";
            return style.ToString();
        }

        private string GetStretchName(FontStretch stretch)
        {
            if (stretch == FontStretches.UltraCondensed)
                return "超紧缩";
            if (stretch == FontStretches.ExtraCondensed)
                return "特紧缩";
            if (stretch == FontStretches.Condensed)
                return "紧缩";
            if (stretch == FontStretches.SemiCondensed)
                return "半紧缩";
            if (stretch == FontStretches.Normal)
                return "常规";
            if (stretch == FontStretches.SemiExpanded)
                return "半扩展";
            if (stretch == FontStretches.Expanded)
                return "扩展";
            if (stretch == FontStretches.ExtraExpanded)
                return "特扩展";
            if (stretch == FontStretches.UltraExpanded)
                return "超扩展";
            if (stretch == FontStretches.Medium)
                return "中等";
            return stretch.ToString();
        }

        public override string ToString() => DisplayName;
    }
}