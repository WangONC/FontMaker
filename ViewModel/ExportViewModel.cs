using CommunityToolkit.Mvvm.ComponentModel;
using FontMaker.Data.Models;
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
        private readonly Dictionary<string, Action> _exportHandlers = new();
        
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
            _exportHandlers["C文件 (.c/.h)"] = () => ExportToCFile(_currentFontData!, _currentFontName!, _currentExportPath);
            _exportHandlers["BIN文件 (.bin)"] = () => ExportToBinFile(_currentFontData!, _currentFontName!, _currentExportPath);
            _exportHandlers["TXT文件 (.txt)"] = () => ExportToTXTFile(_currentFontData!, _currentFontName!, _currentExportPath);
            _exportHandlers["JSON文件 (.json)"] = () => ExportToJSONFile(_currentFontData!, _currentFontName!, _currentExportPath);
            _exportHandlers["XML文件 (.xml)"] = () => ExportToXMLFile(_currentFontData!, _currentFontName!, _currentExportPath);
            _exportHandlers["HEX文件 (.hex)"] = () => ExportToHEXFile(_currentFontData!, _currentFontName!, _currentExportPath);
            _exportHandlers["Arduino代码 (.ino)"] = () => ExportToINOFile(_currentFontData!, _currentFontName!, _currentExportPath);
        }

        /// <summary>
        /// 执行导出操作
        /// </summary>
        /// <param name="fontRenderer">字体渲染器</param>
        /// <param name="charsetManager">字符集管理器</param>
        /// <param name="fontName">字体名称</param>
        /// <param name="exportPath">导出路径（可选，如果为空则弹出保存对话框）</param>
        public void ExecuteExport(BitmapFontRenderer fontRenderer, CharsetManager charsetManager, string fontName, string? exportPath = null)
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

            // 调用对应的导出处理器，传递数据
            InvokeExportHandler(handler, fontData, fontName, exportPath);
        }

        /// <summary>
        /// 调用导出处理器的内部方法
        /// </summary>
        private void InvokeExportHandler(Action handler, FontBitmapData fontData, string fontName, string? exportPath)
        {
            // 将当前导出数据临时存储，供各个导出方法使用
            _currentFontData = fontData;
            _currentFontName = fontName;
            _currentExportPath = exportPath;
            
            try
            {
                handler.Invoke();
            }
            finally
            {
                _currentFontData = null;
                _currentFontName = null;
                _currentExportPath = null;
            }
        }

        private void ExportToCFile(FontBitmapData fontData, string fontName, string? exportPath)
        {
            fontData.CreateReadGuide().GenerateReadInstructions();
            var (offset, length) = fontData.GetCharacterDataPosition(52);
            var a = fontData.Metadata;
            var b = fontData.Characters[52];
            byte[] aa = b.GetBitmapBytes(); // 修复：使用 GetBitmapBytes() 获取 byte[]
            NotificationUtils.showMessage("成功", "已导出为.c");
        }
        
        private void ExportToBinFile(FontBitmapData fontData, string fontName, string? exportPath) 
        { 
            NotificationUtils.showMessage("成功", "已导出为.bin"); 
        }
        
        private void ExportToTXTFile(FontBitmapData fontData, string fontName, string? exportPath) 
        { 
            NotificationUtils.showMessage("成功", "已导出为.txt"); 
        }
        
        private void ExportToJSONFile(FontBitmapData fontData, string fontName, string? exportPath) 
        { 
            NotificationUtils.showMessage("成功", "已导出为.json"); 
        }
        
        private void ExportToXMLFile(FontBitmapData fontData, string fontName, string? exportPath) 
        { 
            NotificationUtils.showMessage("成功", "已导出为.xml"); 
        }
        
        private void ExportToHEXFile(FontBitmapData fontData, string fontName, string? exportPath) 
        { 
            NotificationUtils.showMessage("成功", "已导出为.hex"); 
        }
        
        private void ExportToINOFile(FontBitmapData fontData, string fontName, string? exportPath) 
        { 
            NotificationUtils.showMessage("成功", "已导出为.ino"); 
        }

    }
}
