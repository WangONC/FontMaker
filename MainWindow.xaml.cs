using FontMaker.Data;
using FontMaker.Data.Models;
using FontMaker.Utils;
using FontMaker.ViewModel;
using FontMaker.ViewModels;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Text;
using System.Threading.Tasks;
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
        private FontFamily? fontFamily; // 当前字体家族
        private FontStyle? fontStyle;
        private FontWeight? fontWeight;
        private FontStretch? fontStretch;
        private Typeface? typeface; // 当前字体样式
        private int CurrentFontSize { get; set; } = 16; // 默认字体大小
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
        private bool isHighBitFirst { get; set; } = true; // 是否从左到右排列位（true=从左到右，false=从右到左）
        private bool isFixedWidth { get; set; } = true; // 是否固定宽度

        private int grayLeve { get; set; } = 1; // 灰度级别，默认1级


        // 长按相关字段
        private System.Windows.Threading.DispatcherTimer _longPressTimer = new();
        private DateTime _mouseDownTime = DateTime.Now;
        private bool _isLongPressing = false;
        private string _longPressDirection = ""; // "prev" 或 "next"
        // 长按参数现在使用全局配置

        // 拖动预览相关字段
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private double _previewOffsetX = 0;
        private double _previewOffsetY = 0;

        // 防止字符输入框循环更新的标志
        private bool _isUpdatingCharacterInput = false;


        public MainWindow()
        {
            // 程序启动时加载配置
            SettingsWindow.InitializeAppConfig();
            
            // 初始化语言设置
            FontMaker.ViewModel.LanguagesViewModel.Initialize();
            
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
            
            // 从Config设置默认值
            InitializeControlsFromConfig();

            // 初始化字体渲染器
            InitializeFontRenderer();

            // 初始化预览交互功能
            InitializePreviewInteraction();
        }

        // 从Config初始化控件默认值
        private void InitializeControlsFromConfig()
        {
            // 设置像素尺寸默认值
            widthNumberBox.Value = Config.DefaultPixelWidth;
            heightNumberBox.Value = Config.DefaultPixelHeight;
            PixelSizeWidth = (uint)Config.DefaultPixelWidth;
            PixelSizeHeight = (uint)Config.DefaultPixelHeight;
            
            // 设置偏移量默认值
            horizontalSpaceNumberBox.Value = Config.DefaultHorizontalOffset;
            verticalSpaceNumberBox.Value = Config.DefaultVerticalOffset;
            horizontalSpace = Config.DefaultHorizontalOffset;
            verticalSpace = Config.DefaultVerticalOffset;
            
            // 设置字体大小默认值
            fontSize.Value = Config.DefaultFontSize;
            CurrentFontSize = Config.DefaultFontSize;
            
            // 设置扫描方式默认值
            horizontalScanRadio.IsChecked = Config.DefaultIsHorizontalScan;
            verticalScanRadio.IsChecked = !Config.DefaultIsHorizontalScan;
            isShorizontalScan = Config.DefaultIsHorizontalScan;
            
            // 设置位序默认值
            highBitFirstRadio.IsChecked = Config.DefaultIsHighBitFirst;
            lowBitFirstRadio.IsChecked = !Config.DefaultIsHighBitFirst;
            isHighBitFirst = Config.DefaultIsHighBitFirst;
            
            // 设置宽度模式默认值
            fixedWidthRadio.IsChecked = Config.DefaultIsFixedWidth;
            variableWidthRadio.IsChecked = !Config.DefaultIsFixedWidth;
            isFixedWidth = Config.DefaultIsFixedWidth;
            
            // 设置灰度级别默认值（位数转换为滑块值）
            int sliderValue;
            switch (Config.DefaultBitsPerPixel)
            {
                case 1: sliderValue = 0; break;
                case 2: sliderValue = 1; break;
                case 4: sliderValue = 2; break;
                case 8: sliderValue = 3; break;
                default: sliderValue = 0; break; // 默认1位
            }
            grayLevelSlider.Value = sliderValue;
            grayLeve = Config.DefaultBitsPerPixel;
            
            // 设置缩放默认值
            zoomSlider.Value = Config.DefaultPreviewZoom;
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
            string initialFontFamily = fontFamily?.Source ?? "Microsoft Sans Serif"; // 使用系统默认字体
            _fontRenderer = new BitmapFontRenderer(
                (int)PixelSizeWidth,
                (int)PixelSizeHeight,
                initialFontFamily,
                CurrentFontSize,
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

        // 编辑自定义字符集事件处理
        private void EditCustomCharsetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建字符集编辑器窗口
                var editorWindow = new CharsetEditorWindow();
                editorWindow.Owner = this;

                if (editorWindow.ShowDialog() == true)
                {
                    // 用户保存了新字符集
                    var newCharset = editorWindow.CharsetManager;
                    if (newCharset != null)
                    {
                        // 添加到导入字符集列表
                        ViewModel.CharsetVM.ImportedCharsets?.Add(newCharset);
                        ViewModel.CharsetVM.AllCharsets?.Add(newCharset);

                        // 设置为当前选中的字符集
                        ViewModel.CharsetVM.SelectedCharset = newCharset;

                        // 显示成功通知
                        string message = $"{newCharset.Name}";
                        NotificationUtils.showSuccessNotification(this.SnackbarPresenter,
                            FontMaker.Resources.Lang.Languages.ImportCharSetSuccess, message, 3000, null);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationUtils.showErrorNotification(this.SnackbarPresenter,
                    FontMaker.Resources.Lang.Languages.Error, ex.Message, 5000, null);
            }
        }

        // 浏览自定义字符集文件事件处理
        private void BrowseCustomCharsetButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件对话框选择字符集文件，支持多选
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = FontMaker.Resources.Lang.Languages.CharSetFilter,
                Title = FontMaker.Resources.Lang.Languages.SelectCharSetFile,
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
                            failedFiles.Add($"{System.IO.Path.GetFileName(filePath)}: {FontMaker.Resources.Lang.Languages.InvalidCharSetOrExists}");
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
                        ? string.Format(FontMaker.Resources.Lang.Languages.ImportedCharSetSingle, importedCharsets.First())
                        : string.Format(FontMaker.Resources.Lang.Languages.ImportedCharSetMultiple, importedCharsets.Count);

                    NotificationUtils.showSuccessNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.ImportCharSetSuccess, message, 3000, null);
                }

                if (failedFiles.Count > 0)
                {
                    string errorMessage = string.Format(FontMaker.Resources.Lang.Languages.ImportFailedFilesList, string.Join("\n", failedFiles));
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.PartialCharSetImportFail, errorMessage, 10000, null);
                }

                if (importedCharsets.Count == 0)
                {
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.ImportCharSetFail, FontMaker.Resources.Lang.Languages.NoCharSetImported, 5000, null);
                }
            }
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Max(Config.MinZoomScale, zoomSlider.Value - Config.ZoomStep);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Min(Config.MaxZoomScale, zoomSlider.Value + Config.ZoomStep);
        }

        private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置缩放倍数
            zoomSlider.Value = Config.DefaultPreviewZoom;
            
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
            
            // 将滑块值转换为位深度和显示文本，只支持标准位深度
            int sliderValue = (int)e.NewValue;
            switch (sliderValue)
            {
                case 0:
                    grayLeve = 1;  // 1位深度 = 2级 (黑白)
                    grayLevelText.Text = FontMaker.Resources.Lang.Languages.OneBitText;
                    break;
                case 1:
                    grayLeve = 2;  // 2位深度 = 4级灰度
                    grayLevelText.Text = FontMaker.Resources.Lang.Languages.TwoBitText;
                    break;
                case 2:
                    grayLeve = 4;  // 4位深度 = 16级灰度
                    grayLevelText.Text = FontMaker.Resources.Lang.Languages.FourBitText;
                    break;
                case 3:
                    grayLeve = 8;  // 8位深度 = 256级灰度
                    grayLevelText.Text = FontMaker.Resources.Lang.Languages.EightBitText;
                    break;
                default:
                    grayLeve = 1;
                    grayLevelText.Text = FontMaker.Resources.Lang.Languages.OneBitText;
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
                
                // 立即更新typeface以获得正确的字体信息
                if (fontFamily != null)
                {
                    var currentStyle = fontStyle ?? System.Windows.FontStyles.Normal;
                    var currentWeight = fontWeight ?? System.Windows.FontWeights.Normal;
                    var currentStretch = fontStretch ?? System.Windows.FontStretches.Normal;
                    typeface = new Typeface(fontFamily, currentStyle, currentWeight, currentStretch);
                }
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
                
                // 如果没有选择字体，则抛出异常
                if (fontFamily == null)
                    throw new InvalidOperationException(FontMaker.Resources.Lang.Languages.UnselectedFontError);
                
                string fontFamilyName = fontFamily.Source;
                
                _fontRenderer.UpdateRenderParameters(
                    (int)PixelSizeWidth,
                    (int)PixelSizeHeight,
                    fontFamilyName,
                    CurrentFontSize,
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
                Filter = FontMaker.Resources.Lang.Languages.FontFileFilter,
                Title = FontMaker.Resources.Lang.Languages.SelectFontFile,
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
                            throw new InvalidOperationException(FontMaker.Resources.Lang.Languages.InvalidFontFile);
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

                    string message = importedFonts.Count == 1
                        ? string.Format(FontMaker.Resources.Lang.Languages.ImportedFontSingle, System.IO.Path.GetFileName(fontPath))
                        : string.Format(FontMaker.Resources.Lang.Languages.ImportedFontMultiple, importedFonts.Count);

                    NotificationUtils.showSuccessNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.importFontSuccess, message, 3000, null);
                }

                if (failedFiles.Count > 0)
                {
                    string errorMessage = string.Format(FontMaker.Resources.Lang.Languages.ImportFailedFilesList, string.Join("\n", failedFiles));
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.PartialFontImportFail, errorMessage, 10000, null);
                }

                if (importedFonts.Count == 0)
                {
                    fontPath = string.Empty;
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
                CurrentFontSize = (int)fontSize.Value.Value;
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

            if (elapsed.TotalMilliseconds >= Config.LongPressThreshold)
            {
                if (!_isLongPressing)
                {
                    // 首次进入长按状态
                    _isLongPressing = true;
                    // 调整定时器间隔为快速滚动间隔
                    _longPressTimer.Interval = TimeSpan.FromMilliseconds(Config.FastScrollInterval);
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
            bool isLeftToRightChecked = highBitFirstRadio.IsChecked == true;
            bool isRightToLeftChecked = lowBitFirstRadio.IsChecked == true;

            if (isLeftToRightChecked && !isRightToLeftChecked)
            {
                isHighBitFirst = true; // 从左到右
            }
            else if (!isLeftToRightChecked && isRightToLeftChecked)
            {
                isHighBitFirst = false; // 从右到左
            }
            else
            {
                isHighBitFirst = true; // 默认从左到右
            }
        }

        private void FixedRadioCheckedChanged(object sender, RoutedEventArgs e)
        {
            if(fixedWidthRadio == null || variableWidthRadio == null) return;
            bool fixedWidthChecked = fixedWidthRadio.IsChecked == true;
            bool variableWidthChecked = variableWidthRadio.IsChecked == true;

            if (fixedWidthChecked && !variableWidthChecked)
            {
                isFixedWidth = true;
            }
            else if (!fixedWidthChecked && variableWidthChecked)
            {
                isFixedWidth = false;
            }
            else
            {
                isFixedWidth = true; // 默认固定宽度
            }
        }


        // 文本预览按钮点击事件
        private void previewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fontRenderer == null || charsetManager == null)
            {
                NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.PreviewFail, FontMaker.Resources.Lang.Languages.NeedSelectFontCharset, 3000, null);
                return;
            }

            try
            {
                // 更新渲染器参数
                UpdateFontRendererParameters();
                
                // 创建并显示预览窗口
                var previewWindow = new TextPreviewWindow();
                previewWindow.SetFontRenderer(_fontRenderer, charsetManager);
                previewWindow.Owner = this;
                previewWindow.Show();
            }
            catch (Exception ex)
            {
                NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.PreviewFail, ex.Message, 5000, null);
            }
        }

        // TODO 最终生成字库
        private async void generateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fontRenderer == null || charsetManager == null)
            {
                NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.OutputFail, FontMaker.Resources.Lang.Languages.NeedSelectFontCharset, 3000, null);
                return;
            }

            try
            {
                // 禁用按钮，显示进度条
                generateButton.IsEnabled = false;
                generateProgressBar.Visibility = Visibility.Visible;
                progressText.Visibility = Visibility.Visible;
                generateProgressBar.IsIndeterminate = true;
                progressText.Text = FontMaker.Resources.Lang.Languages.OutputFontLibing;

                // 更新渲染器参数
                UpdateFontRendererParameters();
                
                // 获取字体名称
                string fontName = "UnknownFont";
                try
                {
                    if (fontFamily != null)
                    {
                        // 确保typeface是最新的
                        var currentTypeface = typeface ?? new Typeface(fontFamily, 
                            fontStyle ?? System.Windows.FontStyles.Normal,
                            fontWeight ?? System.Windows.FontWeights.Normal,
                            fontStretch ?? System.Windows.FontStretches.Normal);
                            
                        var fontInfo = new FontUtils(currentTypeface);
                        fontName = fontInfo.GetFamilyName();
                        
                        // 如果获取失败，使用备用方案
                        if (string.IsNullOrEmpty(fontName))
                        {
                            string source = fontFamily.Source;
                            if (source.Contains("#"))
                            {
                                fontName = source.Split('#')[1]; // 提取URI中的字体名
                            }
                            else
                            {
                                fontName = source;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    fontName = fontFamily?.Source ?? "UnknownFont";
                }
                
                // 异步执行导出
                bool result = await Task.Run(() => 
                {
                    return ViewModel.ExportVM.ExecuteExport(_fontRenderer, charsetManager, fontName);
                });

                // 隐藏进度条，恢复按钮
                generateProgressBar.Visibility = Visibility.Collapsed;
                progressText.Visibility = Visibility.Collapsed;
                generateButton.IsEnabled = true;

                if (!result)
                {
                    NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.OutputFail, FontMaker.Resources.Lang.Languages.NoValidOutputFile, 5000, null);
                }
                else
                {
                    NotificationUtils.showSuccessNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.OutputSuccess, FontMaker.Resources.Lang.Languages.FontLibGenerated, 3000, null);
                }
            }
            catch (Exception ex)
            {
                // 出错时也要恢复UI状态
                generateProgressBar.Visibility = Visibility.Collapsed;
                progressText.Visibility = Visibility.Collapsed;
                generateButton.IsEnabled = true;
                
                NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.OutputFail, ex.Message, 5000, null);
            }
        }

        // 字符输入框文本变化事件处理（输入过程中不处理，避免干扰IME）
        private void CharacterInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 输入过程中不处理，让IME能够正常工作
            // 实际处理在LostFocus和KeyDown事件中进行
        }

        // 字符输入框失去焦点时处理（截断为一个字符并查找）
        private void CharacterInputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessCharacterInput(sender as Wpf.Ui.Controls.TextBox);
        }

        // 字符输入框按键处理（按回车时处理输入）
        private void CharacterInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                ProcessCharacterInput(sender as Wpf.Ui.Controls.TextBox);
                // 移除焦点，避免继续输入
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        // 处理字符输入的核心逻辑
        private void ProcessCharacterInput(Wpf.Ui.Controls.TextBox? textBox)
        {
            // 如果是程序内部更新，忽略此事件
            if (_isUpdatingCharacterInput)
                return;

            if (textBox == null || charsetManager == null || totalCharCount == 0)
                return;

            // 如果输入为空，恢复到当前字符
            if (string.IsNullOrEmpty(textBox.Text))
            {
                if (currentCharIndex >= 0 && currentCharIndex < totalCharCount)
                {
                    _isUpdatingCharacterInput = true;
                    textBox.Text = charsetManager.GetChar(currentCharIndex).ToString();
                    _isUpdatingCharacterInput = false;
                }
                return;
            }

            // 截断为一个字符（支持中文等多字节字符）
            string inputText = textBox.Text;
            char inputChar = inputText[0];

            // 更新文本框为单个字符
            if (inputText.Length > 1)
            {
                _isUpdatingCharacterInput = true;
                textBox.Text = inputChar.ToString();
                _isUpdatingCharacterInput = false;
            }

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
            // 保存当前配置
            SettingsWindow.SaveConfigToFile();
            
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
                double delta = e.Delta > 0 ? Config.ZoomStep : -Config.ZoomStep;
                double newValue = Math.Max(Config.MinZoomScale, 
                                  Math.Min(Config.MaxZoomScale, zoomSlider.Value + delta));
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

        // 设置按钮点击事件
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                
                if (settingsWindow.ShowDialog() == true)
                {
                    // 设置已保存，重新初始化控件以应用新设置
                    InitializeControlsFromConfig();
                    
                    // 更新字体渲染器参数
                    UpdateFontRendererParameters();
                    
                    // 更新预览
                    UpdateCharacterPreview();
                    
                    NotificationUtils.showSuccessNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.SetingUpdated, FontMaker.Resources.Lang.Languages.NewSettingUpdate2UI, 3000, null);
                }
            }
            catch (Exception ex)
            {
                NotificationUtils.showErrorNotification(this.SnackbarPresenter, FontMaker.Resources.Lang.Languages.OpenSettingWinFail, ex.Message, 5000, null);
            }
        }
    }
}