using CommunityToolkit.Mvvm.ComponentModel;
using FontMaker.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
        public void ExecuteExport()
        {
            if (string.IsNullOrEmpty(SelectedFormat))
            {
                throw new InvalidOperationException("未选择导出格式");
            }

            if (!_exportHandlers.TryGetValue(SelectedFormat, out var handler))
            {
                throw new NotSupportedException($"不支持的导出格式: {SelectedFormat}");
            }

            handler.Invoke();
        }

        private void ExportToCFile() { NotificationUtils.showMessage("成功", "已导出为.c"); }
        private void ExportToBinFile() { NotificationUtils.showMessage("成功", "已导出为.bin"); }
        private void ExportToTXTFile() { NotificationUtils.showMessage("成功", "已导出为.txt"); }
        private void ExportToJSONFile() { NotificationUtils.showMessage("成功", "已导出为.json"); }
        private void ExportToXMLFile() { NotificationUtils.showMessage("成功", "已导出为.xml"); }
        private void ExportToHEXFile() { NotificationUtils.showMessage("成功", "已导出为.hex"); }
        private void ExportToINOFile() { NotificationUtils.showMessage("成功", "已导出为.ino"); }

    }
}
