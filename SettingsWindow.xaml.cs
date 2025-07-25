using FontMaker.Data;
using FontMaker.Utils;
using FontMaker.ViewModel;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace FontMaker
{
    /// <summary>
    /// 设置窗口
    /// </summary>
    public partial class SettingsWindow : FluentWindow
    {
        private const string ConfigFileName = "FontMakerSettings.json";
        private static string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

        public ExportViewModel ExportViewModel { get; } = new ExportViewModel();

        public SettingsWindow()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(
                    this,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true
                );
            };
            this.DataContext = ExportViewModel;

            // 从当前Config加载到UI
            LoadConfigToUI();
        }

        /// <summary>
        /// 从Config静态类加载值到UI控件
        /// </summary>
        private void LoadConfigToUI()
        {
            // 字体渲染设置
            defaultPixelWidthBox.Value = Config.DefaultPixelWidth;
            defaultPixelHeightBox.Value = Config.DefaultPixelHeight;
            defaultFontSizeBox.Value = Config.DefaultFontSize;
            defaultHorizontalOffsetBox.Value = Config.DefaultHorizontalOffset;
            defaultVerticalOffsetBox.Value = Config.DefaultVerticalOffset;

            // 设置位深度下拉框
            SetBitsPerPixelComboBox(Config.DefaultBitsPerPixel);

            defaultIsHorizontalScanBox.IsChecked = Config.DefaultIsHorizontalScan;
            defaultIsHighBitFirstBox.IsChecked = Config.DefaultIsHighBitFirst;
            defaultIsFixedWidthBox.IsChecked = Config.DefaultIsFixedWidth;

            // 预览设置
            defaultPreviewZoomBox.Value = Config.DefaultPreviewZoom;
            textPreviewLineSpacingBox.Value = Config.TextPreviewLineSpacing;
            minZoomScaleBox.Value = Config.MinZoomScale;
            maxZoomScaleBox.Value = Config.MaxZoomScale;
            zoomStepBox.Value = Config.ZoomStep;

            // 导出设置
            SetExportFormatComboBox(Config.DefaultExportFormat);
            defaultExportDirectoryBox.Text = Config.DefaultExportDirectory;
            defaultExportFileNameBox.Text = Config.DefaultExportFileName;
            isRemoveUnsupportCharBox.IsChecked = Config.IsRemoveUnsupportChar;

            // 界面设置
            languageComboBox.SelectedIndex = 0; // 默认简体中文
            longPressThresholdBox.Value = Config.LongPressThreshold;
            fastScrollIntervalBox.Value = Config.FastScrollInterval;
        }

        /// <summary>
        /// 设置位深度下拉框选中项
        /// </summary>
        private void SetBitsPerPixelComboBox(int bitsPerPixel)
        {
            foreach (ComboBoxItem item in defaultBitsPerPixelBox.Items)
            {
                if (item.Tag?.ToString() == bitsPerPixel.ToString())
                {
                    defaultBitsPerPixelBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// 设置导出格式下拉框选中项
        /// </summary>
        private void SetExportFormatComboBox(string format)
        {
            foreach (ComboBoxItem item in defaultExportFormatBox.Items)
            {
                if (item.Content?.ToString() == format)
                {
                    defaultExportFormatBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// 从UI控件保存值到Config静态类
        /// </summary>
        private void SaveUIToConfig()
        {
            // 字体渲染设置
            if (defaultPixelWidthBox.Value.HasValue)
                Config.DefaultPixelWidth = (int)defaultPixelWidthBox.Value.Value;
            if (defaultPixelHeightBox.Value.HasValue)
                Config.DefaultPixelHeight = (int)defaultPixelHeightBox.Value.Value;
            if (defaultFontSizeBox.Value.HasValue)
                Config.DefaultFontSize = (int)defaultFontSizeBox.Value.Value;
            if (defaultHorizontalOffsetBox.Value.HasValue)
                Config.DefaultHorizontalOffset = (int)defaultHorizontalOffsetBox.Value.Value;
            if (defaultVerticalOffsetBox.Value.HasValue)
                Config.DefaultVerticalOffset = (int)defaultVerticalOffsetBox.Value.Value;

            // 位深度
            if (defaultBitsPerPixelBox.SelectedItem is ComboBoxItem bppItem && bppItem.Tag != null)
            {
                Config.DefaultBitsPerPixel = int.Parse(bppItem.Tag.ToString()!);
            }

            Config.DefaultIsHorizontalScan = defaultIsHorizontalScanBox.IsChecked ?? true;
            Config.DefaultIsHighBitFirst = defaultIsHighBitFirstBox.IsChecked ?? true;
            Config.DefaultIsFixedWidth = defaultIsFixedWidthBox.IsChecked ?? true;

            // 预览设置
            if (defaultPreviewZoomBox.Value.HasValue)
                Config.DefaultPreviewZoom = defaultPreviewZoomBox.Value.Value;
            if (textPreviewLineSpacingBox.Value.HasValue)
                Config.TextPreviewLineSpacing = textPreviewLineSpacingBox.Value.Value;
            if (minZoomScaleBox.Value.HasValue)
                Config.MinZoomScale = minZoomScaleBox.Value.Value;
            if (maxZoomScaleBox.Value.HasValue)
                Config.MaxZoomScale = maxZoomScaleBox.Value.Value;
            if (zoomStepBox.Value.HasValue)
                Config.ZoomStep = zoomStepBox.Value.Value;

            // 导出设置
            if (defaultExportFormatBox.SelectedItem is ExportViewModel formatItem)
            {
                Config.DefaultExportFormat = formatItem.ToString() ?? "C文件 (.c/.h)";
            }
            Config.DefaultExportDirectory = defaultExportDirectoryBox.Text ?? "";
            Config.DefaultExportFileName = defaultExportFileNameBox.Text ?? "{FontName}_{CharsetName}";
            Config.IsRemoveUnsupportChar = isRemoveUnsupportCharBox.IsChecked ?? false;

            // 界面设置
            if (longPressThresholdBox.Value.HasValue)
                Config.LongPressThreshold = (int)longPressThresholdBox.Value.Value;
            if (fastScrollIntervalBox.Value.HasValue)
                Config.FastScrollInterval = (int)fastScrollIntervalBox.Value.Value;
        }

        /// <summary>
        /// 保存设置到JSON文件
        /// </summary>
        public static void SaveConfigToFile(string? filePath = null)
        {
            try
            {
                string targetPath = filePath ?? ConfigFilePath;
                var snapshot = Config.CreateSnapshot();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(snapshot, options);
                File.WriteAllText(targetPath, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从JSON文件加载设置
        /// </summary>
        public static bool LoadConfigFromFile(string? filePath = null)
        {
            try
            {
                string targetPath = filePath ?? ConfigFilePath;
                if (!File.Exists(targetPath))
                {
                    return false;
                }

                string jsonString = File.ReadAllText(targetPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var snapshot = JsonSerializer.Deserialize<ConfigSnapshot>(jsonString, options);
                if (snapshot != null)
                {
                    Config.RestoreFromSnapshot(snapshot);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置文件失败: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 验证配置文件是否完整匹配当前设置选项
        /// </summary>
        public static bool ValidateConfigFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // 尝试反序列化，如果成功说明格式匹配
                var snapshot = JsonSerializer.Deserialize<ConfigSnapshot>(jsonString, options);
                return snapshot != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 初始化应用程序时加载配置
        /// </summary>
        public static void InitializeAppConfig()
        {
            // 如果配置文件不存在，创建默认配置文件
            if (!File.Exists(ConfigFilePath))
            {
                SaveConfigToFile();
            }
            else
            {
                // 加载现有配置文件
                LoadConfigFromFile();
            }
        }

        // 事件处理方法

        private void BrowseDefaultExportDirButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用SaveFileDialog来选择文件夹（间接方式）
            var saveDialog = new SaveFileDialog
            {
                Title = "选择默认导出目录",
                FileName = "选择此文件夹",
                DefaultExt = "folder",
                Filter = "文件夹选择|*.folder"
            };

            if (!string.IsNullOrEmpty(defaultExportDirectoryBox.Text))
            {
                saveDialog.InitialDirectory = defaultExportDirectoryBox.Text;
            }

            if (saveDialog.ShowDialog() == true)
            {
                // 获取选择的目录
                string directory = Path.GetDirectoryName(saveDialog.FileName) ?? "";
                defaultExportDirectoryBox.Text = directory;
            }
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "加载设置文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (ValidateConfigFile(openFileDialog.FileName))
                {
                    if (LoadConfigFromFile(openFileDialog.FileName))
                    {
                        LoadConfigToUI(); // 重新加载UI
                        NotificationUtils.showMessageWPF("提示", "设置文件加载成功！");
                    }
                    else
                    {
                        NotificationUtils.showMessageWPF("提示", "加载设置文件失败！");
                    }
                }
                else
                {
                    NotificationUtils.showMessageWPF("提示", "设置文件格式不正确或不完整！");
                }
            }
        }

        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON配置文件 (*.json)|*.json",
                Title = "导出设置文件",
                FileName = "FontMakerSettings.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 先保存当前UI到Config
                    SaveUIToConfig();
                    // 然后导出到指定文件
                    SaveConfigToFile(saveFileDialog.FileName);
                    NotificationUtils.showMessageWPF("提示","设置文件导出成功！");
                }
                catch (Exception ex)
                {
                    NotificationUtils.showMessageWPF("提示", $"导出设置文件失败：{ex.Message}");
                }
            }
        }

        private void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = NotificationUtils.showMessageWPF(
                "警告",
                "确定要恢复所有设置为默认值吗？这将覆盖当前的所有设置。",
                "是",
                "否",
                null);

            if (result.Result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                Config.ResetToDefaults();
                LoadConfigToUI(); // 重新加载UI显示默认值
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存UI设置到Config
                SaveUIToConfig();
                SaveConfigToFile();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                NotificationUtils.showMessageWPF("错误",$"保存设置失败：{ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}