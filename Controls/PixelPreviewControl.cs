using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FontMaker;

namespace FontMaker.Controls
{
    /// <summary>
    /// 像素预览控件 - 显示字符的点阵预览
    /// </summary>
    public class PixelPreviewControl : UserControl
    {
        private Image _previewImage;
        private BitmapFontRenderer _renderer;
        private char _currentCharacter = 'A';

        // 依赖属性
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(PixelPreviewControl),
                new PropertyMetadata(1.0, OnZoomFactorChanged));

        public static readonly DependencyProperty ShowGridProperty =
            DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(PixelPreviewControl),
                new PropertyMetadata(true, OnShowGridChanged));

        public double ZoomFactor
        {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        public bool ShowGrid
        {
            get => (bool)GetValue(ShowGridProperty);
            set => SetValue(ShowGridProperty, value);
        }

        public PixelPreviewControl()
        {
            InitializeControl();
        }

        private void InitializeControl()
        {
            // 创建预览图像控件
            _previewImage = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // 设置位图缩放模式
            RenderOptions.SetBitmapScalingMode(_previewImage, BitmapScalingMode.NearestNeighbor);

            // 设置内容
            Content = new ScrollViewer
            {
                Content = _previewImage,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Colors.LightGray)
            };

            // 初始化渲染器
            _renderer = new BitmapFontRenderer(16, 16, "Arial", 16);
        }

        /// <summary>
        /// 更新渲染参数
        /// </summary>
        public void UpdateRenderParameters(int width, int height, string fontFamily, int fontSize, 
            System.Drawing.FontStyle fontStyle, int horizontalOffset = 0, int verticalOffset = 0, int bitsPerPixel = 1)
        {
            _renderer?.UpdateRenderParameters(width, height, fontFamily, fontSize, fontStyle);
            
            if (_renderer != null)
            {
                _renderer.HorizontalOffset = horizontalOffset;
                _renderer.VerticalOffset = verticalOffset;
                _renderer.BitsPerPixel = bitsPerPixel;
            }

            RefreshPreview();
        }

        /// <summary>
        /// 设置要预览的字符
        /// </summary>
        public void SetCharacter(char character)
        {
            _currentCharacter = character;
            RefreshPreview();
        }

        /// <summary>
        /// 刷新预览显示
        /// </summary>
        public void RefreshPreview()
        {
            if (_renderer == null) return;

            try
            {
                var previewBitmap = _renderer.RenderCharacterPreview(_currentCharacter);
                
                if (previewBitmap != null)
                {
                    // 应用缩放
                    var scaledBitmap = CreateScaledBitmap(previewBitmap);
                    _previewImage.Source = scaledBitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview refresh failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建缩放后的位图
        /// </summary>
        private BitmapSource CreateScaledBitmap(BitmapSource originalBitmap)
        {
            if (Math.Abs(ZoomFactor - 1.0) < 0.001)
            {
                return originalBitmap;
            }

            int newWidth = (int)(originalBitmap.PixelWidth * ZoomFactor);
            int newHeight = (int)(originalBitmap.PixelHeight * ZoomFactor);

            var transformedBitmap = new TransformedBitmap(originalBitmap,
                new ScaleTransform(ZoomFactor, ZoomFactor));

            return transformedBitmap;
        }

        /// <summary>
        /// 获取当前字符的渲染结果
        /// </summary>
        public CharacterRenderResult GetCurrentRenderResult()
        {
            return _renderer?.RenderCharacter(_currentCharacter);
        }

        /// <summary>
        /// 获取当前字符的二进制数据
        /// </summary>
        public byte[] GetCurrentCharacterData(bool includeWidthInfo = false)
        {
            return _renderer?.GetCharacterBinaryData(_currentCharacter, includeWidthInfo);
        }

        private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PixelPreviewControl control)
            {
                control.RefreshPreview();
            }
        }

        private static void OnShowGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PixelPreviewControl control)
            {
                // TODO: 实现网格显示/隐藏逻辑
                control.RefreshPreview();
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RefreshPreview();
        }

        public void Dispose()
        {
            _renderer?.Dispose();
        }
    }

    /// <summary>
    /// 网格预览控件 - 显示带网格的像素预览
    /// </summary>
    public class GridPixelPreviewControl : Canvas
    {
        private Rectangle[,] _pixelRects;
        private int _pixelWidth = 16;
        private int _pixelHeight = 16;
        private double _cellSize = 20;

        public int PixelWidth
        {
            get => _pixelWidth;
            set
            {
                _pixelWidth = value;
                RecreateGrid();
            }
        }

        public int PixelHeight
        {
            get => _pixelHeight;
            set
            {
                _pixelHeight = value;
                RecreateGrid();
            }
        }

        public double CellSize
        {
            get => _cellSize;
            set
            {
                _cellSize = value;
                RecreateGrid();
            }
        }

        public GridPixelPreviewControl()
        {
            Background = new SolidColorBrush(Colors.White);
            RecreateGrid();
        }

        private void RecreateGrid()
        {
            Children.Clear();
            _pixelRects = new Rectangle[_pixelWidth, _pixelHeight];

            Width = _pixelWidth * _cellSize;
            Height = _pixelHeight * _cellSize;

            // 创建像素矩形
            for (int x = 0; x < _pixelWidth; x++)
            {
                for (int y = 0; y < _pixelHeight; y++)
                {
                    var rect = new Rectangle
                    {
                        Width = _cellSize,
                        Height = _cellSize,
                        Fill = new SolidColorBrush(Colors.Black),
                        Stroke = new SolidColorBrush(Colors.Gray),
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(rect, x * _cellSize);
                    Canvas.SetTop(rect, y * _cellSize);

                    _pixelRects[x, y] = rect;
                    Children.Add(rect);
                }
            }
        }

        /// <summary>
        /// 更新像素数据显示
        /// </summary>
        public void UpdatePixelData(byte[] pixelData, int bitsPerPixel = 1)
        {
            if (_pixelRects == null || pixelData == null) return;

            int pixelsPerByte = 8 / bitsPerPixel;
            int maxValue = (1 << bitsPerPixel) - 1;

            for (int y = 0; y < _pixelHeight && y < _pixelHeight; y++)
            {
                for (int x = 0; x < _pixelWidth && x < _pixelWidth; x++)
                {
                    // 计算像素在数据中的位置
                    int pixelIndex = y * _pixelWidth + x;
                    int byteIndex = pixelIndex / pixelsPerByte;
                    int bitOffset = (pixelIndex % pixelsPerByte) * bitsPerPixel;

                    if (byteIndex < pixelData.Length)
                    {
                        // 提取像素值
                        int pixelValue = (pixelData[byteIndex] >> (8 - bitOffset - bitsPerPixel)) & maxValue;
                        
                        // 转换为灰度颜色
                        byte grayValue = (byte)((pixelValue * 255) / maxValue);
                        var color = Color.FromRgb(grayValue, grayValue, grayValue);
                        
                        _pixelRects[x, y].Fill = new SolidColorBrush(color);
                    }
                }
            }
        }
    }
}