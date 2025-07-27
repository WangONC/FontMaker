using CommunityToolkit.Mvvm.ComponentModel;
using FontMaker.Data;
using FontMaker.Data.Models;
using FontMaker.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FontMaker.ViewModel
{
    public partial class ExportViewModel : ObservableObject
    {
        // 可用格式列表
        [ObservableProperty]
        private ObservableCollection<string>? _availableFormats;

        [ObservableProperty]
        private string _selectedFormat = string.Empty;

        // 导出处理器映射字典
        private readonly Dictionary<string, Func<FontBitmapData, string, string, BitmapFontRenderer, CharsetManager, string?, bool>> _exportHandlers = new();
        
        // 当前导出数据（供导出方法使用）
        private FontBitmapData? _currentFontData;
        private string? _currentFontName;
        private string? _currentExportPath;

        public ExportViewModel()
        {
            // 初始化可用格式
            _availableFormats = new ObservableCollection<string>();
            InitializeFormats();

            // 初始化导出处理器映射
            InitializeExportHandlers();
        }

        private void InitializeFormats()
        {
            // 确保AvailableFormats不为null
            if (AvailableFormats == null)
            {
                AvailableFormats = new ObservableCollection<string>();
            }

            AvailableFormats.Add("C (.c/.h)");
            AvailableFormats.Add("BIN (.bin)");
            AvailableFormats.Add("TXT (.txt)");
            AvailableFormats.Add("JSON (.json)");
            AvailableFormats.Add("XML (.xml)");
            AvailableFormats.Add("HEX (.hex)");
            AvailableFormats.Add("Arduino (.ino)");

            // 设置默认选中项
            SelectedFormat = AvailableFormats.FirstOrDefault() ?? string.Empty;
        }

        private void InitializeExportHandlers()
        {
            _exportHandlers["C (.c/.h)"] = ExportToCFile;
            _exportHandlers["BIN (.bin)"] = ExportToBinFile;
            _exportHandlers["TXT (.txt)"] = ExportToTXTFile;
            _exportHandlers["JSON (.json)"] = ExportToJSONFile;
            _exportHandlers["XML (.xml)"] = ExportToXMLFile;
            _exportHandlers["HEX (.hex)"] = ExportToHEXFile;
            _exportHandlers["Arduino (.ino)"] = ExportToINOFile;
        }

        /// <summary>
        /// 执行导出操作
        /// </summary>
        /// <param name="fontRenderer">字体渲染器</param>
        /// <param name="charsetManager">字符集管理器</param>
        /// <param name="fontName">字体名称</param>
        /// <param name="exportPath">导出路径（可选，如果为空则弹出保存对话框）</param>
        /// <returns>导出是否成功</returns>
        public bool ExecuteExport(BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string fontName, string? exportPath = null)
        {
            if (string.IsNullOrEmpty(SelectedFormat))
            {
                throw new InvalidOperationException(FontMaker.Resources.Lang.Languages.NoExportFormatSelected);
            }

            if (fontRenderer == null)
            {
                throw new ArgumentNullException(nameof(fontRenderer), FontMaker.Resources.Lang.Languages.FontRendererCannotBeNull);
            }

            if (charsetManager == null)
            {
                throw new ArgumentNullException(nameof(charsetManager), FontMaker.Resources.Lang.Languages.CharsetManagerCannotBeNull);
            }

            // 生成字体数据
            var fontData = fontRenderer.RenderCharset(charsetManager);

            if (!_exportHandlers.TryGetValue(SelectedFormat, out var handler))
            {
                throw new NotSupportedException(string.Format(FontMaker.Resources.Lang.Languages.UnsupportedExportFormat, SelectedFormat));
            }

            // 调用对应的导出处理器
            return handler(fontData, fontName, charsetManager.Name ?? "Unknown", fontRenderer, charsetManager, exportPath);
        }

        private bool ExportToCFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath)
        {
            // 使用Config的文件名模板
            string defaultFileName = Config.ReplaceFileNameVariables(
                Config.DefaultExportFileName,
                fontName: fontName,
                charsetName: charsetName,
                fontSize: fontRenderer.FontSize,
                width: fontRenderer.Width,
                height: fontRenderer.Height,
                bitsPerPixel: fontRenderer.BitsPerPixel,
                isHorizontalScan: fontRenderer.IsHorizontalScan,
                isHighBitFirst: fontRenderer.IsHighBitFirst,
                isFixedWidth: fontRenderer.IsFixedWidth,
                charCount: charsetManager.CharCount
            ) + ".h";

            string outputPath = exportPath ?? FileUtils.GetSaveFilePath(FontMaker.Resources.Lang.Languages.CHeaderFileFilter, defaultFileName);
            if (string.IsNullOrEmpty(outputPath))
                return false;

            var exporter = new FontMaker.Exporters.CExporter(outputPath);
            return exporter.Export(fontData, fontName);
        }
        
        private bool ExportToBinFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath) 
        { 
            // TODO: 实现BIN文件导出逻辑
            return false; // 暂未实现
        }
        
        private bool ExportToTXTFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath) 
        { 
            // TODO: 实现TXT文件导出逻辑
            return false; // 暂未实现
        }
        
        private bool ExportToJSONFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath) 
        { 
            // TODO: 实现JSON文件导出逻辑
            return false; // 暂未实现
        }
        
        private bool ExportToXMLFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath) 
        { 
            // TODO: 实现XML文件导出逻辑
            return false; // 暂未实现
        }
        
        private bool ExportToHEXFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath) 
        { 
            // TODO: 实现HEX文件导出逻辑
            return false; // 暂未实现
        }
        
        private bool ExportToINOFile(FontBitmapData fontData, string fontName, string charsetName, BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string? exportPath) 
        {
            // 使用Config的文件名模板
            string defaultFileName = Config.ReplaceFileNameVariables(
                Config.DefaultExportFileName,
                fontName: fontName,
                charsetName: charsetName,
                fontSize: fontRenderer.FontSize,
                width: fontRenderer.Width,
                height: fontRenderer.Height,
                bitsPerPixel: fontRenderer.BitsPerPixel,
                isHorizontalScan: fontRenderer.IsHorizontalScan,
                isHighBitFirst: fontRenderer.IsHighBitFirst,
                isFixedWidth: fontRenderer.IsFixedWidth,
                charCount: charsetManager.CharCount
            ) + ".ino";

            string outputPath = exportPath ?? FileUtils.GetSaveFilePath(FontMaker.Resources.Lang.Languages.ArduinoFileFilter, defaultFileName);
            if (string.IsNullOrEmpty(outputPath))
                return false;

            var exporter = new FontMaker.Exporters.CExporter(outputPath);
            return exporter.Export(fontData, fontName);
        }

    }
}
