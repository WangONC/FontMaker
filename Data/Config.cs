using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FontMaker.Data
{
    /// <summary>
    /// 全局静态配置类 - 用于存储应用程序的全局设置
    /// 支持属性变更通知，一处设置全局生效
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// 属性变更事件
        /// </summary>
        public static event PropertyChangedEventHandler? PropertyChanged;

        #region 字体渲染设置

        private static int _defaultPixelWidth = 16;
        /// <summary>
        /// 默认像素宽度
        /// </summary>
        public static int DefaultPixelWidth
        {
            get => _defaultPixelWidth;
            set => SetProperty(ref _defaultPixelWidth, value);
        }

        private static int _defaultPixelHeight = 16;
        /// <summary>
        /// 默认像素高度
        /// </summary>
        public static int DefaultPixelHeight
        {
            get => _defaultPixelHeight;
            set => SetProperty(ref _defaultPixelHeight, value);
        }

        private static int _defaultFontSize = 16;
        /// <summary>
        /// 默认字体大小
        /// </summary>
        public static int DefaultFontSize
        {
            get => _defaultFontSize;
            set => SetProperty(ref _defaultFontSize, value);
        }

        private static int _defaultHorizontalOffset = 0;
        /// <summary>
        /// 默认水平偏移
        /// </summary>
        public static int DefaultHorizontalOffset
        {
            get => _defaultHorizontalOffset;
            set => SetProperty(ref _defaultHorizontalOffset, value);
        }

        private static int _defaultVerticalOffset = 0;
        /// <summary>
        /// 默认垂直偏移
        /// </summary>
        public static int DefaultVerticalOffset
        {
            get => _defaultVerticalOffset;
            set => SetProperty(ref _defaultVerticalOffset, value);
        }

        private static int _defaultBitsPerPixel = 1;
        /// <summary>
        /// 默认每像素位数（灰度级别）
        /// </summary>
        public static int DefaultBitsPerPixel
        {
            get => _defaultBitsPerPixel;
            set => SetProperty(ref _defaultBitsPerPixel, value);
        }

        private static bool _defaultIsHorizontalScan = true;
        /// <summary>
        /// 默认扫描方式（true=水平，false=垂直）
        /// </summary>
        public static bool DefaultIsHorizontalScan
        {
            get => _defaultIsHorizontalScan;
            set => SetProperty(ref _defaultIsHorizontalScan, value);
        }

        private static bool _defaultIsHighBitFirst = true;
        /// <summary>
        /// 默认位序（true=从左到右，false=从右到左）
        /// </summary>
        public static bool DefaultIsHighBitFirst
        {
            get => _defaultIsHighBitFirst;
            set => SetProperty(ref _defaultIsHighBitFirst, value);
        }

        private static bool _defaultIsFixedWidth = true;
        /// <summary>
        /// 默认宽度模式（true=固定宽度，false=可变宽度）
        /// </summary>
        public static bool DefaultIsFixedWidth
        {
            get => _defaultIsFixedWidth;
            set => SetProperty(ref _defaultIsFixedWidth, value);
        }

        private static string _defaultLanguageCode = "zh-CN";
        /// <summary>
        /// 默认语言代码
        /// </summary>
        public static string DefaultLanguageCode
        {
            get => _defaultLanguageCode;
            set => SetProperty(ref _defaultLanguageCode, value);
        }

        #endregion

        #region 预览设置

        private static double _defaultPreviewZoom = 1.0;
        /// <summary>
        /// 默认预览缩放倍数
        /// </summary>
        public static double DefaultPreviewZoom
        {
            get => _defaultPreviewZoom;
            set => SetProperty(ref _defaultPreviewZoom, value);
        }

        private static double _textPreviewLineSpacing = 1.0;
        /// <summary>
        /// 文本预览行间距
        /// </summary>
        public static double TextPreviewLineSpacing
        {
            get => _textPreviewLineSpacing;
            set => SetProperty(ref _textPreviewLineSpacing, value);
        }

        private static double _minZoomScale = 0.1;
        /// <summary>
        /// 最小缩放倍数
        /// </summary>
        public static double MinZoomScale
        {
            get => _minZoomScale;
            set => SetProperty(ref _minZoomScale, value);
        }

        private static double _maxZoomScale = 10.0;
        /// <summary>
        /// 最大缩放倍数
        /// </summary>
        public static double MaxZoomScale
        {
            get => _maxZoomScale;
            set => SetProperty(ref _maxZoomScale, value);
        }

        private static double _zoomStep = 0.1;
        /// <summary>
        /// 缩放步长（滚轮和按钮的每次缩放增减量）
        /// </summary>
        public static double ZoomStep
        {
            get => _zoomStep;
            set => SetProperty(ref _zoomStep, value);
        }

        #endregion

        #region 导出设置

        private static string _defaultExportFormat = "C (.c/.h)";
        /// <summary>
        /// 默认导出格式
        /// </summary>
        public static string DefaultExportFormat
        {
            get => _defaultExportFormat;
            set => SetProperty(ref _defaultExportFormat, value);
        }

        private static bool _isRemoveUnsupportChar = false;
        /// <summary>
        /// 是否移除当前字体不支持的字符，如果设定为false，则字库中所有字符都会被导出，但是不支持的字符会是全空白
        /// </summary>
        public static bool IsRemoveUnsupportChar
        {
            get => _isRemoveUnsupportChar;
            set => SetProperty(ref _isRemoveUnsupportChar, value);
        }

        private static string _defaultExportDirectory = "";
        /// <summary>
        /// 默认导出目录（用户设置的固定导出文件夹）
        /// </summary>
        public static string DefaultExportDirectory
        {
            get => _defaultExportDirectory;
            set => SetProperty(ref _defaultExportDirectory, value);
        }

        private static string _defaultExportFileName = "{FontName}_{CharsetName}";
        /// <summary>
        /// 默认导出文件名模板，支持变量：
        /// {FontName} - 字体名称
        /// {CharsetName} - 字符集名称
        /// {FontSize} - 字体大小
        /// {Width} - 像素宽度
        /// {Height} - 像素高度
        /// {Width}x{Height} - 像素尺寸
        /// {BPP} - 每像素位数（1/2/4/8）
        /// {GrayLevel} - 灰度级别（2/4/16/256级）
        /// {ScanMode} - 扫描方式（H/V，表示水平/垂直）
        /// {BitOrder} - 位序（LR/RL，表示从左到右/从右到左）
        /// {WidthMode} - 宽度模式（Fixed/Variable）
        /// {CharCount} - 字符数量
        /// {DateTime} - 当前日期时间 (yyyyMMdd_HHmmss)
        /// {Date} - 当前日期 (yyyyMMdd)
        /// {Time} - 当前时间 (HHmmss)
        /// {Year} - 年份 (yyyy)
        /// {Month} - 月份 (MM)
        /// {Day} - 日期 (dd)
        /// {Hour} - 小时 (HH)
        /// {Minute} - 分钟 (mm)
        /// {Second} - 秒数 (ss)
        /// </summary>
        public static string DefaultExportFileName
        {
            get => _defaultExportFileName;
            set => SetProperty(ref _defaultExportFileName, value);
        }

        #endregion

        #region 界面设置

        private static int _longPressThreshold = 600;
        /// <summary>
        /// 长按阈值（毫秒），超过此时间触发长按快速滚动
        /// </summary>
        public static int LongPressThreshold
        {
            get => _longPressThreshold;
            set => SetProperty(ref _longPressThreshold, value);
        }

        private static int _fastScrollInterval = 25;
        /// <summary>
        /// 长按快速滚动间隔（毫秒），长按时每隔此时间执行一次字符切换
        /// </summary>
        public static int FastScrollInterval
        {
            get => _fastScrollInterval;
            set => SetProperty(ref _fastScrollInterval, value);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置属性值并触发变更通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否发生了变更</returns>
        private static bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        private static void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public static void ResetToDefaults()
        {
            DefaultPixelWidth = 16;
            DefaultPixelHeight = 16;
            DefaultFontSize = 16;
            DefaultHorizontalOffset = 0;
            DefaultVerticalOffset = 0;
            DefaultBitsPerPixel = 1;
            DefaultIsHorizontalScan = true;
            DefaultIsHighBitFirst = true;
            DefaultIsFixedWidth = true;
            DefaultPreviewZoom = 1.0;
            TextPreviewLineSpacing = 1.0;
            MinZoomScale = 0.1;
            MaxZoomScale = 10.0;
            ZoomStep = 0.1;
            DefaultExportFormat = "C文件 (.c/.h)";
            IsRemoveUnsupportChar = false;
            DefaultExportDirectory = "";
            DefaultExportFileName = "{FontName}_{CharsetName}";
            LongPressThreshold = 600;
            FastScrollInterval = 25;
            DefaultLanguageCode = "zh-CN";
        }

        /// <summary>
        /// 复制当前设置到另一个配置对象（用于备份/恢复）
        /// </summary>
        public static ConfigSnapshot CreateSnapshot()
        {
            return new ConfigSnapshot
            {
                DefaultPixelWidth = DefaultPixelWidth,
                DefaultPixelHeight = DefaultPixelHeight,
                DefaultFontSize = DefaultFontSize,
                DefaultHorizontalOffset = DefaultHorizontalOffset,
                DefaultVerticalOffset = DefaultVerticalOffset,
                DefaultBitsPerPixel = DefaultBitsPerPixel,
                DefaultIsHorizontalScan = DefaultIsHorizontalScan,
                DefaultIsHighBitFirst = DefaultIsHighBitFirst,
                DefaultIsFixedWidth = DefaultIsFixedWidth,
                DefaultPreviewZoom = DefaultPreviewZoom,
                TextPreviewLineSpacing = TextPreviewLineSpacing,
                MinZoomScale = MinZoomScale,
                MaxZoomScale = MaxZoomScale,
                ZoomStep = ZoomStep,
                DefaultExportFormat = DefaultExportFormat,
                IsRemoveUnsupportChar = IsRemoveUnsupportChar,
                DefaultExportDirectory = DefaultExportDirectory,
                DefaultExportFileName = DefaultExportFileName,
                LongPressThreshold = LongPressThreshold,
                FastScrollInterval = FastScrollInterval,
                DefaultLanguageCode = DefaultLanguageCode
            };
        }

        /// <summary>
        /// 替换导出文件名模板中的变量
        /// </summary>
        /// <param name="template">文件名模板</param>
        /// <param name="fontName">字体名称</param>
        /// <param name="charsetName">字符集名称</param>
        /// <param name="fontSize">字体大小</param>
        /// <param name="width">像素宽度</param>
        /// <param name="height">像素高度</param>
        /// <param name="bitsPerPixel">每像素位数</param>
        /// <param name="isHorizontalScan">是否水平扫描</param>
        /// <param name="isHighBitFirst">是否高位在前</param>
        /// <param name="isFixedWidth">是否固定宽度</param>
        /// <param name="charCount">字符数量</param>
        /// <returns>替换后的文件名</returns>
        public static string ReplaceFileNameVariables(
            string template, 
            string fontName = "", 
            string charsetName = "",
            int fontSize = 0,
            int width = 0, 
            int height = 0, 
            int bitsPerPixel = 1,
            bool isHorizontalScan = true,
            bool isHighBitFirst = true,
            bool isFixedWidth = true,
            int charCount = 0)
        {
            string result = template;
            var now = DateTime.Now;
            
            // 基本信息
            result = result.Replace("{FontName}", fontName);
            result = result.Replace("{CharsetName}", charsetName);
            result = result.Replace("{FontSize}", fontSize.ToString());
            result = result.Replace("{Width}", width.ToString());
            result = result.Replace("{Height}", height.ToString());
            result = result.Replace("{Width}x{Height}", $"{width}x{height}");
            result = result.Replace("{CharCount}", charCount.ToString());
            
            // 渲染参数
            result = result.Replace("{BPP}", bitsPerPixel.ToString());
            result = result.Replace("{GrayLevel}", (1 << bitsPerPixel).ToString());
            result = result.Replace("{ScanMode}", isHorizontalScan ? "H" : "V");
            result = result.Replace("{BitOrder}", isHighBitFirst ? "LR" : "RL");
            result = result.Replace("{WidthMode}", isFixedWidth ? "Fixed" : "Variable");
            
            // 日期时间
            result = result.Replace("{DateTime}", now.ToString("yyyyMMdd_HHmmss"));
            result = result.Replace("{Date}", now.ToString("yyyyMMdd"));
            result = result.Replace("{Time}", now.ToString("HHmmss"));
            result = result.Replace("{Year}", now.ToString("yyyy"));
            result = result.Replace("{Month}", now.ToString("MM"));
            result = result.Replace("{Day}", now.ToString("dd"));
            result = result.Replace("{Hour}", now.ToString("HH"));
            result = result.Replace("{Minute}", now.ToString("mm"));
            result = result.Replace("{Second}", now.ToString("ss"));
            
            return result;
        }

        /// <summary>
        /// 从快照恢复设置
        /// </summary>
        public static void RestoreFromSnapshot(ConfigSnapshot snapshot)
        {
            DefaultPixelWidth = snapshot.DefaultPixelWidth;
            DefaultPixelHeight = snapshot.DefaultPixelHeight;
            DefaultFontSize = snapshot.DefaultFontSize;
            DefaultHorizontalOffset = snapshot.DefaultHorizontalOffset;
            DefaultVerticalOffset = snapshot.DefaultVerticalOffset;
            DefaultBitsPerPixel = snapshot.DefaultBitsPerPixel;
            DefaultIsHorizontalScan = snapshot.DefaultIsHorizontalScan;
            DefaultIsHighBitFirst = snapshot.DefaultIsHighBitFirst;
            DefaultIsFixedWidth = snapshot.DefaultIsFixedWidth;
            DefaultPreviewZoom = snapshot.DefaultPreviewZoom;
            TextPreviewLineSpacing = snapshot.TextPreviewLineSpacing;
            MinZoomScale = snapshot.MinZoomScale;
            MaxZoomScale = snapshot.MaxZoomScale;
            ZoomStep = snapshot.ZoomStep;
            DefaultExportFormat = snapshot.DefaultExportFormat;
            IsRemoveUnsupportChar = snapshot.IsRemoveUnsupportChar;
            DefaultExportDirectory = snapshot.DefaultExportDirectory;
            DefaultExportFileName = snapshot.DefaultExportFileName;
            LongPressThreshold = snapshot.LongPressThreshold;
            FastScrollInterval = snapshot.FastScrollInterval;
            DefaultLanguageCode = snapshot.DefaultLanguageCode;
        }

        #endregion
    }

    /// <summary>
    /// 配置快照类 - 用于备份和恢复配置
    /// </summary>
    public class ConfigSnapshot
    {
        public int DefaultPixelWidth { get; set; }
        public int DefaultPixelHeight { get; set; }
        public int DefaultFontSize { get; set; }
        public int DefaultHorizontalOffset { get; set; }
        public int DefaultVerticalOffset { get; set; }
        public int DefaultBitsPerPixel { get; set; }
        public bool DefaultIsHorizontalScan { get; set; }
        public bool DefaultIsHighBitFirst { get; set; }
        public bool DefaultIsFixedWidth { get; set; }
        public double DefaultPreviewZoom { get; set; }
        public double TextPreviewLineSpacing { get; set; }
        public double MinZoomScale { get; set; }
        public double MaxZoomScale { get; set; }
        public double ZoomStep { get; set; }
        public string DefaultExportFormat { get; set; } = "";
        public bool IsRemoveUnsupportChar { get; set; }
        public string DefaultExportDirectory { get; set; } = "";
        public string DefaultExportFileName { get; set; } = "";
        public int LongPressThreshold { get; set; }
        public int FastScrollInterval { get; set; }
        public string DefaultLanguageCode { get; set; } = "";
    }
}