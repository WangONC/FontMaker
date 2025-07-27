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
                NotificationUtils.showMessageError(FontMaker.Resources.Lang.Languages.Error, string.Format(FontMaker.Resources.Lang.Languages.LoadFontVariantError, ex.Message));
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

                if (weight != FontMaker.Resources.Lang.Languages.FontStyleRegular) parts.Add(weight);
                if (style != FontMaker.Resources.Lang.Languages.FontStyleRegular) parts.Add(style);
                if (stretch != FontMaker.Resources.Lang.Languages.FontStyleRegular) parts.Add(stretch);

                return parts.Any() ? string.Join(", ", parts) : FontMaker.Resources.Lang.Languages.FontStyleRegular;
            }
        }

        private string GetWeightName(FontWeight weight)
        {
            var weightValue = weight.ToOpenTypeWeight();
            return weightValue switch
            {
                // 细体
                <= 100 => FontMaker.Resources.Lang.Languages.FontWeightThin,
                <= 150 => FontMaker.Resources.Lang.Languages.FontWeightExtraLight,
                <= 200 => FontMaker.Resources.Lang.Languages.FontWeightUltraLight,
                <= 250 => FontMaker.Resources.Lang.Languages.FontWeightSemiLight,
                <= 300 => FontMaker.Resources.Lang.Languages.FontWeightLight,

                // 常规
                350 => FontMaker.Resources.Lang.Languages.FontWeightLight,
                400 => FontMaker.Resources.Lang.Languages.FontStyleRegular,
                450 => FontMaker.Resources.Lang.Languages.FontWeightMedium,

                // 中等
                500 => FontMaker.Resources.Lang.Languages.FontWeightMedium,
                550 => FontMaker.Resources.Lang.Languages.FontWeightSemiBold,

                // 粗体
                600 => FontMaker.Resources.Lang.Languages.FontWeightSemiBold,
                650 => FontMaker.Resources.Lang.Languages.FontWeightSemiBold,
                700 => FontMaker.Resources.Lang.Languages.FontWeightBold,

                // 特粗
                750 => FontMaker.Resources.Lang.Languages.FontWeightExtraBold,
                800 => FontMaker.Resources.Lang.Languages.FontWeightExtraBold,
                850 => FontMaker.Resources.Lang.Languages.FontWeightExtraBold,
                // 极粗
                900 => FontMaker.Resources.Lang.Languages.FontWeightBlack,

                _ => weight.ToString()
            };
        }

        private string GetStyleName(FontStyle style)
        {
            if (style == FontStyles.Normal)
                return FontMaker.Resources.Lang.Languages.FontStyleRegular;
            if (style == FontStyles.Italic)
                return FontMaker.Resources.Lang.Languages.FontStyleItalic;
            if (style == FontStyles.Oblique)
                return FontMaker.Resources.Lang.Languages.FontStyleOblique;
            return style.ToString();
        }

        private string GetStretchName(FontStretch stretch)
        {
            if (stretch == FontStretches.UltraCondensed)
                return FontMaker.Resources.Lang.Languages.FontStretchUltraCondensed;
            if (stretch == FontStretches.ExtraCondensed)
                return FontMaker.Resources.Lang.Languages.FontStretchExtraCondensed;
            if (stretch == FontStretches.Condensed)
                return FontMaker.Resources.Lang.Languages.FontStretchCondensed;
            if (stretch == FontStretches.SemiCondensed)
                return FontMaker.Resources.Lang.Languages.FontStretchSemiCondensed;
            if (stretch == FontStretches.Normal)
                return FontMaker.Resources.Lang.Languages.FontStyleRegular;
            if (stretch == FontStretches.SemiExpanded)
                return FontMaker.Resources.Lang.Languages.FontStretchSemiExpanded;
            if (stretch == FontStretches.Expanded)
                return FontMaker.Resources.Lang.Languages.FontStretchExpanded;
            if (stretch == FontStretches.ExtraExpanded)
                return FontMaker.Resources.Lang.Languages.FontStretchExtraExpanded;
            if (stretch == FontStretches.UltraExpanded)
                return FontMaker.Resources.Lang.Languages.FontStretchUltraExpanded;
            if (stretch == FontStretches.Medium)
                return FontMaker.Resources.Lang.Languages.FontWeightMedium;
            return stretch.ToString();
        }

        public override string ToString() => DisplayName;
    }
}