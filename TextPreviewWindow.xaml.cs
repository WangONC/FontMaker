using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using FontMaker.Data;
using FontMaker.Data.Models;

namespace FontMaker
{
    /// <summary>
    /// 文本预览窗口
    /// </summary>
    public partial class TextPreviewWindow : Wpf.Ui.Controls.FluentWindow
    {
        private BitmapFontRenderer? _fontRenderer;
        private CharsetManager? _charsetManager;
        private double _currentZoom = 1.0;
        private double _availableWidth = 400; // 可用宽度，用于自动换行计算

        public TextPreviewWindow()
        {
            InitializeComponent();
            
            // 从Config初始化默认值
            _currentZoom = Config.DefaultPreviewZoom;
            
            // 添加滚轮缩放事件
            previewCanvas.MouseWheel += PreviewCanvas_MouseWheel;
            
            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(
                    this,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true
                );
            };

            previewTextBox.Text= @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
0123456789
`!@#$%^&*()-=_+
点阵字库生成器";
        }

        /// <summary>
        /// 设置字体渲染器和字符集
        /// </summary>
        public void SetFontRenderer(BitmapFontRenderer fontRenderer, CharsetManager charsetManager)
        {
            _fontRenderer = fontRenderer;
            _charsetManager = charsetManager;
            UpdatePreview();
        }

        private void PreviewTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
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
            zoomSlider.Value = Config.DefaultPreviewZoom;
            previewScrollViewer.ScrollToHorizontalOffset(0);
            previewScrollViewer.ScrollToVerticalOffset(0);
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currentZoom = e.NewValue;
            UpdatePreview();
        }

        private void PreviewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _availableWidth = Math.Max(200, e.NewSize.Width - 40); // 减去边距
            UpdatePreview();
        }

        private void PreviewCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 滚轮缩放
            if (zoomSlider != null)
            {
                double delta = e.Delta > 0 ? Config.ZoomStep : -Config.ZoomStep;
                double newValue = Math.Max(Config.MinZoomScale, 
                                  Math.Min(Config.MaxZoomScale, zoomSlider.Value + delta));
                zoomSlider.Value = newValue;
                
                // 标记事件已处理，防止ScrollViewer也响应滚轮事件
                e.Handled = true;
            }
        }

        /// <summary>
        /// 更新文本预览
        /// </summary>
        private void UpdatePreview()
        {
            if (_fontRenderer == null || _charsetManager == null)
                return;

            string text = previewTextBox.Text ?? "";
            if (string.IsNullOrEmpty(text))
            {
                previewCanvas.Children.Clear();
                previewCanvas.Width = _availableWidth;
                previewCanvas.Height = 100;
                return;
            }

            try
            {
                // 清空画布
                previewCanvas.Children.Clear();

                // 计算布局参数
                int charWidth = _fontRenderer.Width;
                int charHeight = _fontRenderer.Height;
                double scaledCharWidth = charWidth * _currentZoom;
                double scaledCharHeight = charHeight * _currentZoom;

                const double leftMargin = 10;
                const double topMargin = 10;
                double lineSpacing = Config.TextPreviewLineSpacing; // 使用全局配置的行间距
                
                double currentX = leftMargin;
                double currentY = topMargin;
                double maxLineHeight = scaledCharHeight;
                double maxX = currentX; // 记录最大X坐标

                // 逐字符渲染，实现自动换行
                foreach (char ch in text)
                {
                    // 手动换行符处理
                    if (ch == '\n')
                    {
                        currentX = leftMargin;
                        currentY += scaledCharHeight; // 直接换行，不添加额外间距
                        maxLineHeight = scaledCharHeight;
                        continue;
                    }
                    
                    // 跳过回车符
                    if (ch == '\r')
                    {
                        continue;
                    }

                    // 空格处理
                    if (ch == ' ')
                    {
                        double spaceWidth = scaledCharWidth * 0.5; // 空格宽度为字符宽度的一半
                        
                        // 检查是否需要换行
                        if (currentX + spaceWidth > _availableWidth - leftMargin)
                        {
                            currentX = leftMargin;
                            currentY += scaledCharHeight + lineSpacing; // 自动换行保持行间距
                            maxLineHeight = scaledCharHeight;
                        }
                        else
                        {
                            currentX += spaceWidth;
                        }
                        continue;
                    }

                    // 检查字符是否在字符集中
                    if (!_charsetManager.ContainsChar(ch))
                    {
                        // 字符不在字符集中，显示占位符
                        double placeholderWidth = scaledCharWidth * 0.3;
                        
                        // 检查是否需要换行
                        if (currentX + placeholderWidth > _availableWidth - leftMargin)
                        {
                            currentX = leftMargin;
                            currentY += scaledCharHeight + lineSpacing; // 自动换行保持行间距
                            maxLineHeight = scaledCharHeight;
                        }
                        
                        // 创建占位符（小方块）
                        var placeholder = new System.Windows.Shapes.Rectangle
                        {
                            Width = placeholderWidth,
                            Height = scaledCharHeight * 0.8,
                            Fill = new SolidColorBrush(Colors.LightGray),
                            Stroke = new SolidColorBrush(Colors.Gray),
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(placeholder, currentX);
                        Canvas.SetTop(placeholder, currentY + scaledCharHeight * 0.1);
                        previewCanvas.Children.Add(placeholder);

                        currentX += placeholderWidth + 2; // 添加小间距
                        continue;
                    }

                    // 检查是否需要自动换行
                    if (currentX + scaledCharWidth > _availableWidth - leftMargin)
                    {
                        currentX = leftMargin;
                        currentY += scaledCharHeight + lineSpacing; // 自动换行保持行间距
                        maxLineHeight = scaledCharHeight;
                    }

                    // 渲染字符
                    var charBitmap = _fontRenderer.RenderCharacterPreview(ch);
                    if (charBitmap != null)
                    {
                        var charImage = new System.Windows.Controls.Image
                        {
                            Source = charBitmap,
                            Width = charWidth,
                            Height = charHeight,
                            Stretch = Stretch.Fill
                        };

                        // 设置像素化渲染
                        RenderOptions.SetBitmapScalingMode(charImage, BitmapScalingMode.NearestNeighbor);

                        // 应用缩放
                        var scaleTransform = new ScaleTransform(_currentZoom, _currentZoom);
                        charImage.RenderTransform = scaleTransform;

                        // 设置位置
                        Canvas.SetLeft(charImage, currentX);
                        Canvas.SetTop(charImage, currentY);

                        // 添加到画布
                        previewCanvas.Children.Add(charImage);

                        // 更新位置
                        currentX += scaledCharWidth;
                        maxX = Math.Max(maxX, currentX);
                    }
                }

                // 更新画布大小
                previewCanvas.Width = Math.Max(_availableWidth, maxX + leftMargin);
                previewCanvas.Height = Math.Max(100, currentY + maxLineHeight + topMargin);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Text preview update failed: {ex.Message}");
            }
        }
    }
}