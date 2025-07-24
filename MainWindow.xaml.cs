using FontMaker.Data.Models;
using FontMaker.ViewModel;
using FontMaker.ViewModels;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace FontMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainViewModel ViewModel { get; }
        public String fontPath { get; set; } = string.Empty; // 当前选中的字体路径
        private bool isLocalFont = false; // 是否为本地字体
        private FontFamily? fontFamily; // 当前字体家族
        private FontStyle? fontStyle;
        private FontWeight? fontWeight;
        private FontStretch? fontStretch;
        private Typeface? typeface; // 当前字体样式
        private int FontSize { get; set; } = 16; // 默认字体大小
        private CharsetManager? charsetManager; // 当前字符集

        private int currentCharIndex = 0; // 当前字符索引
        private int totalCharCount = 0; // 字符集总字符数

        // 渲染相关
        private BitmapFontRenderer? _fontRenderer;

        private uint PixelSizeWidth { get; set; } = 16; // 默认像素宽度
        private uint PixelSizeHeight { get; set; } = 16; // 默认像素高度
        private int horizontalSpace { get; set; } = 0; // 水平间距
        private int verticalSpace { get; set; } = 0; // 垂直间距

        private bool isShorizontalScan { get; set; } = true; // 是否水平扫描
        private bool isHighBitFirst { get; set; } = false; // 是否高位在前
        private bool isFixedWidth { get; set; } = false; // 是否固定宽度

        private int grayLeve { get; set; } = 1; // 灰度级别，默认1级


        // 长按相关字段
        private System.Windows.Threading.DispatcherTimer _longPressTimer;
        private DateTime _mouseDownTime;
        private bool _isLongPressing = false;
        private string _longPressDirection = ""; // "prev" 或 "next"
        private const int LONG_PRESS_THRESHOLD_MS = 600; // 长按阈值，600毫秒
        private const int FAST_SCROLL_INTERVAL_MS = 25; // 快速滚动间隔，25毫秒

        // 拖动预览相关字段
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private double _previewOffsetX = 0;
        private double _previewOffsetY = 0;

        // 防止字符输入框循环更新的标志
        private bool _isUpdatingCharacterInput = false;


        public MainWindow()
        {
            ViewModel = new MainViewModel();
            InitializeComponent();
            this.DataContext = ViewModel;

            // 初始化长按定时器
            InitializeLongPressTimer();

            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(
                    this,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true
                );
            };
            // 初始化像素网格预览（预留）
            InitializePixelGrid();

            // 初始化字体渲染器
            InitializeFontRenderer();

            // 初始化预览交互功能
            InitializePreviewInteraction();
        }

        // 初始化长按定时器
        private void InitializeLongPressTimer()
        {
            _longPressTimer = new System.Windows.Threading.DispatcherTimer();
            _longPressTimer.Interval = TimeSpan.FromMilliseconds(10); // 每10ms检查一次
            _longPressTimer.Tick += LongPressTimer_Tick;
        }

        // 初始化字体渲染器
        private void InitializeFontRenderer()
        {
            _fontRenderer = new BitmapFontRenderer(
                (int)PixelSizeWidth, 
                (int)PixelSizeHeight, 
                "Arial", 
                FontSize, 
                System.Drawing.FontStyle.Regular);
        }

        // 初始化预览交互功能
        private void InitializePreviewInteraction()
        {
            // 添加鼠标事件处理器
            pixelCanvas.MouseLeftButtonDown += PixelCanvas_MouseLeftButtonDown;
            pixelCanvas.MouseLeftButtonUp += PixelCanvas_MouseLeftButtonUp;
            pixelCanvas.MouseMove += PixelCanvas_MouseMove;
            pixelCanvas.MouseWheel += PixelCanvas_MouseWheel;
            pixelCanvas.MouseLeave += PixelCanvas_MouseLeave;
        }

        // 初始化像素网格（预留方法）
        private void InitializePixelGrid()
        {
            // TODO: 根据默认的16x16像素创建网格
            // CreatePixelGrid(16, 16);
        }

        // 创建像素网格（预留方法）
        private void CreatePixelGrid(int width, int height)
        {
            // TODO: 实现像素网格创建逻辑
            // 1. 清空现有网格
            // pixelGridContainer.Children.Clear();
            // 
            // 2. 根据宽度和高度创建网格
            // double pixelSize = 20; // 每个像素的显示大小
            // 
            // 3. 绘制网格线
            // for (int i = 0; i <= width; i++) { ... }
            // for (int j = 0; j <= height; j++) { ... }
            // 
            // 4. 创建像素点
            // for (int x = 0; x < width; x++) {
            //     for (int y = 0; y < height; y++) { ... }
            // }
            //
            // 5. 更新画布尺寸
            // pixelCanvas.Width = width * pixelSize;
            // pixelCanvas.Height = height * pixelSize;
        }

        private void CharsetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            charsetManager = charsetComboBox.SelectedItem as CharsetManager;
            if (charsetManager != null && charsetManager.GetCharCount() != 0)
            {
                totalCharCount = charsetManager.GetCharCount();
                currentCharIndex = 0; // 重置当前字符索引
                NavigateToCharacter(0);
            }
        }

        // 浏览自定义字符集文件事件处理
        private void BrowseCustomCharsetButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件对话框选择字符集文件，支持多选
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "字符集文件 (*.cst)|*.cst|所有文件 (*.*)|*.*",
                Title = "选择字符集文件",
                Multiselect = true // 启用多选
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var importedCharsets = new List<string>();
                var failedFiles = new List<string>();

                foreach (string filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        // 使用CharsetService导入字符集
                        bool success = ViewModel.CharsetVM.AddImportedCharset(filePath);

                        if (success && filePath != null)
                        {
                            string name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                            importedCharsets.Add(name);
                        }
                        else
                        {
                            failedFiles.Add($"{System.IO.Path.GetFileName(filePath)}: 无效的字符集文件或已存在同名字符集");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"{System.IO.Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                // 显示导入结果
                if (importedCharsets.Count > 0)
                {
                    string message = importedCharsets.Count == 1
                        ? $"已导入字符集: {importedCharsets.First()}"
                        : $"成功导入 {importedCharsets.Count} 个字符集";

                    NotificationUtils.showSuccessNotification(this.SnackbarPresenter, "导入字符集成功", message, 3000, null);
                }

                if (failedFiles.Count > 0)
                {
                    string errorMessage = "以下文件导入失败:\n" + string.Join("\n", failedFiles);
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, "部分字符集导入失败", errorMessage, 10000, null);
                }

                if (importedCharsets.Count == 0)
                {
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, "导入字符集失败", "没有成功导入任何字符集文件", 5000, null);
                }
            }
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - 0.1);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + 0.1);
        }

        private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置缩放倍数
            zoomSlider.Value = 1.0;
            
            // 重置位置到正中央
            _previewOffsetX = 0;
            _previewOffsetY = 0;
            
            // 更新预览显示
            UpdateCharacterPreview();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 应用缩放变化到预览画布
            if (pixelCanvas != null)
            {
                UpdateCharacterPreview(); // 重新渲染以应用缩放
            }
        }

        private void PixelSizeChanged(object sender, Wpf.Ui.Controls.NumberBoxValueChangedEventArgs e)
        {
            if (widthNumberBox == null || heightNumberBox == null) return;
            if (widthNumberBox.Value.HasValue && heightNumberBox.Value.HasValue)
            {
                PixelSizeWidth = (uint)widthNumberBox.Value.Value;
                PixelSizeHeight = (uint)heightNumberBox.Value.Value;
                
                // 更新预览
                UpdateCharacterPreview();
            }
        }

        private void SpaceSizeChanged(object sender, Wpf.Ui.Controls.NumberBoxValueChangedEventArgs e)
        {
            if (horizontalSpaceNumberBox == null || verticalSpaceNumberBox == null) return;
            if (horizontalSpaceNumberBox.Value.HasValue && verticalSpaceNumberBox.Value.HasValue)
            {
                horizontalSpace = (int)horizontalSpaceNumberBox.Value.Value;
                verticalSpace = (int)verticalSpaceNumberBox.Value.Value;
                
                // 更新预览
                UpdateCharacterPreview();
            }
        }

        // 灰度级别滑块变化事件处理
        private void GrayLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(grayLevelText == null) return;
            
            // 将滑块值转换为位深度和显示文本
            int sliderValue = (int)e.NewValue;
            switch (sliderValue)
            {
                case 0:
                    grayLeve = 1;  // 1位深度 = 2级 (黑白)
                    grayLevelText.Text = "2级";
                    break;
                case 1:
                    grayLeve = 2;  // 2位深度 = 4级灰度
                    grayLevelText.Text = "4级";
                    break;
                case 2:
                    grayLeve = 3;  // 3位深度 = 8级灰度
                    grayLevelText.Text = "8级";
                    break;
                case 3:
                    grayLeve = 4;  // 4位深度 = 16级灰度
                    grayLevelText.Text = "16级";
                    break;
                case 4:
                    grayLeve = 5;  // 5位深度 = 32级灰度
                    grayLevelText.Text = "32级";
                    break;
                case 5:
                    grayLeve = 6;  // 6位深度 = 64级灰度
                    grayLevelText.Text = "64级";
                    break;
                case 6:
                    grayLeve = 7;  // 7位深度 = 128级灰度
                    grayLevelText.Text = "128级";
                    break;
                case 7:
                    grayLeve = 8;  // 8位深度 = 256级灰度
                    grayLevelText.Text = "256级";
                    break;
                default:
                    grayLeve = 1;
                    grayLevelText.Text = "2级";
                    break;
            }
            
            // 更新预览
            UpdateCharacterPreview();
        }

        // 字体选择变化事件处理
        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontComboBox.SelectedItem is FontFamily selectedFont)
            {
                fontFamily = selectedFont;
            }

            // 更新字体渲染样式
            UpdateCharacterPreview();
        }

        // 字符导航事件处理
        private void FirstCharButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToCharacter(0);
        }

      

        private void LastCharButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToCharacter(totalCharCount - 1);
        }

        private bool NavigateToCharacter(int index)
        {
            if (charsetManager == null || index < 0 || index >= totalCharCount)
                return false;
            currentCharIndex = index;
            currentCharInfoTextBox.Text = charsetManager.GetCharInfo(index);
            
            // 设置标志防止循环更新
            _isUpdatingCharacterInput = true;
            characterInputTextBox.Text = charsetManager.GetChar(index).ToString();
            _isUpdatingCharacterInput = false;
            
            // 更新字符渲染预览
            UpdateCharacterPreview();
            
            return true;
        }

        /// <summary>
        /// 更新字符预览显示
        /// </summary>
        private void UpdateCharacterPreview()
        {
            if (_fontRenderer == null || charsetManager == null || currentCharIndex < 0 || currentCharIndex >= totalCharCount)
                return;

            try
            {
                char currentChar = charsetManager.GetChar(currentCharIndex);
                
                // 更新渲染器参数
                UpdateFontRendererParameters();
                
                // 渲染字符预览
                var previewBitmap = _fontRenderer.RenderCharacterPreview(currentChar);
                
                // 将previewBitmap显示到pixelCanvas中
                if (previewBitmap != null)
                {
                    DisplayPreviewOnCanvas(previewBitmap);
                }
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Character preview update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 在Canvas上显示预览图像
        /// </summary>
        private void DisplayPreviewOnCanvas(BitmapSource previewBitmap)
        {
            // 清空Canvas上的现有内容
            pixelCanvas.Children.Clear();

            // 获取当前缩放比例
            double zoomFactor = zoomSlider?.Value ?? 1.0;

            // 创建Image控件显示预览
            var previewImage = new System.Windows.Controls.Image
            {
                Source = previewBitmap,
                Stretch = System.Windows.Media.Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 设置位图缩放模式为最邻近插值（保持像素化效果）
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(previewImage, BitmapScalingMode.NearestNeighbor);

            // 应用缩放变换
            var scaleTransform = new ScaleTransform(zoomFactor, zoomFactor);
            previewImage.RenderTransform = scaleTransform;

            // 计算缩放后的尺寸用于居中
            double scaledWidth = previewBitmap.PixelWidth * zoomFactor;
            double scaledHeight = previewBitmap.PixelHeight * zoomFactor;

            // 基础居中位置
            double baseCenterX = (pixelCanvas.ActualWidth - scaledWidth) / 2;
            double baseCenterY = (pixelCanvas.ActualHeight - scaledHeight) / 2;
            
            // 应用拖动偏移量
            double finalX = baseCenterX + scaledWidth / 2 - previewBitmap.PixelWidth / 2 + _previewOffsetX;
            double finalY = baseCenterY + scaledHeight / 2 - previewBitmap.PixelHeight / 2 + _previewOffsetY;
            
            // 设置原始图像大小
            previewImage.Width = previewBitmap.PixelWidth;
            previewImage.Height = previewBitmap.PixelHeight;
            
            Canvas.SetLeft(previewImage, finalX);
            Canvas.SetTop(previewImage, finalY);

            // 添加到Canvas
            pixelCanvas.Children.Add(previewImage);
        }

        /// <summary>
        /// 更新字体渲染器参数
        /// </summary>
        private void UpdateFontRendererParameters()
        {
            if (_fontRenderer == null) return;

            try
            {
                // 转换WPF FontStyle到GDI+ FontStyle
                var gdiFontStyle = ConvertToGdiFontStyle();
                
                string fontFamilyName = fontFamily?.Source ?? "Arial";
                
                _fontRenderer.UpdateRenderParameters(
                    (int)PixelSizeWidth,
                    (int)PixelSizeHeight,
                    fontFamilyName,
                    FontSize,
                    gdiFontStyle);

                _fontRenderer.HorizontalOffset = horizontalSpace;
                _fontRenderer.VerticalOffset = verticalSpace;
                _fontRenderer.BitsPerPixel = grayLeve;
                _fontRenderer.IsHorizontalScan = isShorizontalScan;
                _fontRenderer.IsHighBitFirst = isHighBitFirst;
                _fontRenderer.IsFixedWidth = isFixedWidth;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Font renderer parameters update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 转换WPF FontStyle到GDI+ FontStyle
        /// </summary>
        private System.Drawing.FontStyle ConvertToGdiFontStyle()
        {
            var style = System.Drawing.FontStyle.Regular;

            if (fontWeight.HasValue && fontWeight.Value.ToOpenTypeWeight() >= 600)
            {
                style |= System.Drawing.FontStyle.Bold;
            }

            if (fontStyle.HasValue && fontStyle.Value == System.Windows.FontStyles.Italic)
            {
                style |= System.Drawing.FontStyle.Italic;
            }

            return style;
        }

        private void importFontButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开filePicker，筛选字体，支持多选
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "字体文件 (*.ttf;*.otf)|*.ttf;*.otf|所有文件 (*.*)|*.*",
                Title = "选择字体文件",
                Multiselect = true // 启用多选
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var importedFonts = new List<FontFamily>();
                var failedFiles = new List<string>();

                foreach (string filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        PrivateFontCollection fontCollection = new PrivateFontCollection();
                        fontCollection.AddFontFile(filePath);
                        var newFontFamily = new FontFamily($"file:///{filePath}#{fontCollection.Families[0].Name}");

                        // 验证字体是否有效
                        if (!newFontFamily.GetTypefaces().Any())
                        {
                            throw new InvalidOperationException("无效的字体文件");
                        }

                        // 添加到ViewModel中
                        ViewModel.FontVM.AddImportedFont(newFontFamily, filePath);
                        importedFonts.Add(newFontFamily);
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"{System.IO.Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                // 显示导入结果
                if (importedFonts.Count > 0)
                {
                    // 设置第一个导入的字体为当前选中
                    fontFamily = importedFonts.First();
                    fontPath = openFileDialog.FileNames.First();
                    isLocalFont = true;

                    string message = importedFonts.Count == 1
                        ? $"已导入字体: {System.IO.Path.GetFileName(fontPath)}"
                        : $"成功导入 {importedFonts.Count} 个字体";

                    NotificationUtils.showSuccessNotification(this.SnackbarPresenter, "导入字体成功", message, 3000, null);
                }

                if (failedFiles.Count > 0)
                {
                    string errorMessage = "以下文件导入失败:\n" + string.Join("\n", failedFiles);
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, "部分字体导入失败", errorMessage, 10000, null);
                }

                if (importedFonts.Count == 0)
                {
                    fontPath = string.Empty;
                    isLocalFont = false;
                    fontFamily = null;
                }

                // 更新字体渲染样式
            UpdateCharacterPreview();
            }
        }

        private void fontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (fontSize.Value.HasValue)
            {
                FontSize = (int)fontSize.Value.Value;
            }
            // 更新预览显示
            UpdateCharacterPreview();
        }

        private void fontStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontStyleComboBox.SelectedItem is TypefaceInfo selectedType)
            {
                fontStyle = selectedType.Typeface.Style;
                fontWeight = selectedType.Typeface.Weight;
                fontStretch = selectedType.Typeface.Stretch;
                typeface = new Typeface(fontFamily, selectedType.Typeface.Style, selectedType.Typeface.Weight, selectedType.Typeface.Stretch);

                // 更新预览显示
                UpdateCharacterPreview();

            }
        }

        private void LongPressTimer_Tick(object? sender, EventArgs e)
        {

            var elapsed = DateTime.Now - _mouseDownTime;

            if (elapsed.TotalMilliseconds >= LONG_PRESS_THRESHOLD_MS)
            {
                if (!_isLongPressing)
                {
                    // 首次进入长按状态
                    _isLongPressing = true;
                    // 调整定时器间隔为快速滚动间隔
                    _longPressTimer.Interval = TimeSpan.FromMilliseconds(FAST_SCROLL_INTERVAL_MS);
                }

                // 执行快速滚动
                if (_longPressDirection == "prev")
                {
                    if (currentCharIndex > 0)
                    {
                        NavigateToCharacter(currentCharIndex - 1);
                    }
                }
                else if (_longPressDirection == "next")
                {
                    if (currentCharIndex < totalCharCount - 1)
                    {
                        NavigateToCharacter(currentCharIndex + 1);
                    }
                }
            }
        }

        // 长按事件处理
        private void prevCharButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentCharIndex <= 0)
                return; // 如果已经是第一个字符，则不处理
            if (charsetManager == null)
                return; // 如果字符集未加载，则不处理
            NavigateToCharacter(currentCharIndex - 1);

            _mouseDownTime = DateTime.Now;
            _longPressDirection = "prev";
            _isLongPressing = false;
            _longPressTimer.Interval = TimeSpan.FromMilliseconds(10); // 重置为检查间隔
            _longPressTimer.Start();

            // 捕获鼠标，确保能收到抬起事件
            ((UIElement)sender).CaptureMouse();
        }

        private void nextCharButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentCharIndex >= totalCharCount)
                return;
            if (charsetManager == null)
                return;
            NavigateToCharacter(currentCharIndex + 1);


            _mouseDownTime = DateTime.Now;
            _longPressDirection = "next";
            _isLongPressing = false;
            _longPressTimer.Interval = TimeSpan.FromMilliseconds(10); // 重置为检查间隔
            _longPressTimer.Start();

            // 捕获鼠标，确保能收到抬起事件
            ((UIElement)sender).CaptureMouse();
        }

        private void prevCharButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopLongPress();
            ((UIElement)sender).ReleaseMouseCapture();
        }

        private void nextCharButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopLongPress();
            ((UIElement)sender).ReleaseMouseCapture();
        }

        private void StopLongPress()
        {
            _longPressTimer.Stop();
            _isLongPressing = false;
            _longPressDirection = "";
        }

        private void currentCharInfoTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 滚轮方向上时，显示上一个字符信息
            if (e.Delta > 0)
            {
                if (currentCharIndex > 0)
                {
                    NavigateToCharacter(currentCharIndex - 1);
                }
            }
            // 滚轮方向下时，显示下一个字符信息
            else if (e.Delta < 0)
            {
                if (currentCharIndex < totalCharCount - 1)
                {
                    NavigateToCharacter(currentCharIndex + 1);
                }

            }
        }

        private void FirstRadioCheckedChanged(object sender, RoutedEventArgs e)
        {
            if(lowBitFirstRadio == null || highBitFirstRadio == null) return;
            bool isHighBitChecked = highBitFirstRadio.IsChecked == true;
            bool isLowBitChecked = lowBitFirstRadio.IsChecked == true;

            if (isHighBitChecked && !isLowBitChecked)
            {
                isHighBitFirst = true;
            }
            else if (!isHighBitChecked && isLowBitChecked)
            {
                isHighBitFirst = false;
            }
            else
            {
                isHighBitFirst = true; // 默认高位在前
            }
        }

        private void FixedRadioCheckedChanged(object sender, RoutedEventArgs e)
        {
            if(fixedWidthRadio == null || variableWidthRadio == null) return;
            bool fixedWidthChecked = fixedWidthRadio.IsChecked == true;
            bool isLowBitChecked = variableWidthRadio.IsChecked == true;

            if (fixedWidthChecked && !isLowBitChecked)
            {
                fixedWidthChecked = true;
            }
            else if (!fixedWidthChecked && isLowBitChecked)
            {
                fixedWidthChecked = false;
            }
            else
            {
                fixedWidthChecked = true; // 默认高位在前
            }
        }

        // TODO 最终生成字库
        private void generateButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExportVM.ExecuteExport();
        }

        // 字符输入框文本变化事件处理
        private void CharacterInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 如果是程序内部更新，忽略此事件
            if (_isUpdatingCharacterInput)
                return;

            if (charsetManager == null || totalCharCount == 0)
                return;

            var textBox = sender as Wpf.Ui.Controls.TextBox;
            if (textBox == null || string.IsNullOrEmpty(textBox.Text))
                return;

            char inputChar = textBox.Text[0];

            // 在当前字符集中查找输入的字符
            for (int i = 0; i < totalCharCount; i++)
            {
                if (charsetManager.GetChar(i) == inputChar)
                {
                    // 找到字符，跳转到该索引
                    NavigateToCharacter(i);
                    return;
                }
            }

            // 如果字符不在当前字符集中，恢复到当前字符
            if (currentCharIndex >= 0 && currentCharIndex < totalCharCount)
            {
                _isUpdatingCharacterInput = true;
                textBox.Text = charsetManager.GetChar(currentCharIndex).ToString();
                _isUpdatingCharacterInput = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理渲染器资源
            _fontRenderer?.Dispose();
            base.OnClosed(e);
        }

        #region 预览交互事件处理

        private void PixelCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(pixelCanvas);
            pixelCanvas.CaptureMouse();
            pixelCanvas.Cursor = Cursors.Hand;
        }

        private void PixelCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            pixelCanvas.ReleaseMouseCapture();
            pixelCanvas.Cursor = Cursors.Arrow;
        }

        private void PixelCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(pixelCanvas);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;

                _previewOffsetX += deltaX;
                _previewOffsetY += deltaY;

                _lastMousePosition = currentPosition;

                // 重新渲染预览以应用新的偏移量
                UpdateCharacterPreview();
            }
        }

        private void PixelCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 滚轮缩放
            if (zoomSlider != null)
            {
                double delta = e.Delta > 0 ? 0.1 : -0.1;
                double newValue = Math.Max(zoomSlider.Minimum, 
                                  Math.Min(zoomSlider.Maximum, zoomSlider.Value + delta));
                zoomSlider.Value = newValue;
            }
        }

        private void PixelCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                pixelCanvas.ReleaseMouseCapture();
                pixelCanvas.Cursor = Cursors.Arrow;
            }
        }

        #endregion
    }
}