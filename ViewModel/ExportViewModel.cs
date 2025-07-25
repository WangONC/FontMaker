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

            AvailableFormats.Add("C文件 (.c/.h)");
            AvailableFormats.Add("BIN文件 (.bin)");
            AvailableFormats.Add("TXT文件 (.txt)");
            AvailableFormats.Add("JSON文件 (.json)");
            AvailableFormats.Add("XML文件 (.xml)");
            AvailableFormats.Add("HEX文件 (.hex)");
            AvailableFormats.Add("Arduino代码 (.ino)");

            // 设置默认选中项
            SelectedFormat = AvailableFormats.FirstOrDefault() ?? string.Empty;
        }

        private void InitializeExportHandlers()
        {
            _exportHandlers["C文件 (.c/.h)"] = ExportToCFile;
            _exportHandlers["BIN文件 (.bin)"] = ExportToBinFile;
            _exportHandlers["TXT文件 (.txt)"] = ExportToTXTFile;
            _exportHandlers["JSON文件 (.json)"] = ExportToJSONFile;
            _exportHandlers["XML文件 (.xml)"] = ExportToXMLFile;
            _exportHandlers["HEX文件 (.hex)"] = ExportToHEXFile;
            _exportHandlers["Arduino代码 (.ino)"] = ExportToINOFile;
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
                throw new InvalidOperationException("未选择导出格式");
            }

            if (fontRenderer == null)
            {
                throw new ArgumentNullException(nameof(fontRenderer), "字体渲染器不能为空");
            }

            if (charsetManager == null)
            {
                throw new ArgumentNullException(nameof(charsetManager), "字符集管理器不能为空");
            }

            // 生成字体数据
            var fontData = fontRenderer.RenderCharset(charsetManager);

            if (!_exportHandlers.TryGetValue(SelectedFormat, out var handler))
            {
                throw new NotSupportedException($"不支持的导出格式: {SelectedFormat}");
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

            string outputPath = exportPath ?? FileUtils.GetSaveFilePath("C头文件 (*.h)|*.h", defaultFileName);
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

            string outputPath = exportPath ?? FileUtils.GetSaveFilePath("Arduino文件 (*.ino)|*.ino", defaultFileName);
            if (string.IsNullOrEmpty(outputPath))
                return false;

            var exporter = new FontMaker.Exporters.CExporter(outputPath);
            return exporter.Export(fontData, fontName);
        }

    }
}
