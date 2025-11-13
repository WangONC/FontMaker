using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FontMaker.Data.Models;
using FontMaker.Resources.Lang;

namespace FontMaker
{
    /// <summary>
    /// 字符集编辑器窗口
    /// </summary>
    public partial class CharsetEditorWindow : Wpf.Ui.Controls.FluentWindow
    {
        private CharsetManager? _charsetManager;
        private bool _isEditMode;

        /// <summary>
        /// 获取编辑后的字符集管理器
        /// </summary>
        public CharsetManager? CharsetManager => _charsetManager;

        /// <summary>
        /// 创建新字符集的构造函数
        /// </summary>
        public CharsetEditorWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeWindow();
        }

        /// <summary>
        /// 编辑现有字符集的构造函数
        /// </summary>
        /// <param name="charset">要编辑的字符集</param>
        public CharsetEditorWindow(CharsetManager charset)
        {
            InitializeComponent();
            _isEditMode = true;
            _charsetManager = charset;
            InitializeWindow();
            LoadCharset(charset);
        }

        /// <summary>
        /// 初始化窗口
        /// </summary>
        private void InitializeWindow()
        {
            UpdateCharCount();
        }

        /// <summary>
        /// 加载字符集到编辑器
        /// </summary>
        /// <param name="charset">要加载的字符集</param>
        private void LoadCharset(CharsetManager charset)
        {
            charsetNameBox.Text = charset.Name;
            characterInputBox.Text = new string(charset.Characters.ToArray());
            UpdateCharCount();
        }

        /// <summary>
        /// 更新字符计数显示
        /// </summary>
        private void UpdateCharCount()
        {
            var text = characterInputBox.Text ?? string.Empty;
            var uniqueChars = new HashSet<char>(text);
            charCountText.Text = string.Format(Languages.CharCountFormat, uniqueChars.Count);
        }

        /// <summary>
        /// 字符输入框文本改变事件
        /// </summary>
        private void CharacterInputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateCharCount();
        }

        /// <summary>
        /// 添加数字 0-9
        /// </summary>
        private void AddDigitsButton_Click(object sender, RoutedEventArgs e)
        {
            AddCharacters("0123456789");
        }

        /// <summary>
        /// 添加小写字母 a-z
        /// </summary>
        private void AddLowercaseButton_Click(object sender, RoutedEventArgs e)
        {
            AddCharacters("abcdefghijklmnopqrstuvwxyz");
        }

        /// <summary>
        /// 添加大写字母 A-Z
        /// </summary>
        private void AddUppercaseButton_Click(object sender, RoutedEventArgs e)
        {
            AddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        /// <summary>
        /// 添加常用标点符号
        /// </summary>
        private void AddPunctuationButton_Click(object sender, RoutedEventArgs e)
        {
            AddCharacters("!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ ");
        }

        /// <summary>
        /// 添加字符到输入框
        /// </summary>
        /// <param name="characters">要添加的字符</param>
        private void AddCharacters(string characters)
        {
            characterInputBox.Text += characters;
            UpdateCharCount();
        }

        /// <summary>
        /// 去除重复字符
        /// </summary>
        private void RemoveDuplicatesButton_Click(object sender, RoutedEventArgs e)
        {
            var text = characterInputBox.Text;
            if (string.IsNullOrEmpty(text))
                return;

            // 使用 LinkedHashSet 保持顺序并去重
            var uniqueChars = new LinkedHashSet<char>();
            foreach (char ch in text)
            {
                uniqueChars.Add(ch);
            }

            characterInputBox.Text = new string(uniqueChars.ToArray());
            UpdateCharCount();

            statusText.Text = Languages.DuplicatesRemoved;
        }

        /// <summary>
        /// 排序字符
        /// </summary>
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var text = characterInputBox.Text;
            if (string.IsNullOrEmpty(text))
                return;

            // 先去重，再按 Unicode 值排序
            var uniqueChars = new HashSet<char>(text);
            var sortedChars = uniqueChars.OrderBy(c => (int)c).ToArray();

            characterInputBox.Text = new string(sortedChars);
            UpdateCharCount();

            statusText.Text = Languages.CharactersSorted;
        }

        /// <summary>
        /// 清空所有字符
        /// </summary>
        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await NotificationUtils.showMessageWPF(
                Languages.ClearAllConfirmTitle,
                Languages.ClearAllConfirmMessage,
                Languages.Yes,
                Languages.No);

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                characterInputBox.Text = string.Empty;
                UpdateCharCount();
                statusText.Text = Languages.CharactersCleared;
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证字符集名称
            var name = charsetNameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                NotificationUtils.showErrorNotification(
                    SnackbarPresenter,
                    Languages.CharsetNameEmptyTitle,
                    Languages.CharsetNameEmptyMessage,
                    3000);
                charsetNameBox.Focus();
                return;
            }

            // 验证字符内容
            var text = characterInputBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                NotificationUtils.showErrorNotification(
                    SnackbarPresenter,
                    Languages.CharsetEmptyTitle,
                    Languages.CharsetEmptyMessage,
                    3000);
                characterInputBox.Focus();
                return;
            }

            // 创建或更新字符集
            if (_isEditMode && _charsetManager != null)
            {
                // 编辑模式：清空并重新添加字符
                _charsetManager.Delete();
                foreach (char ch in text)
                {
                    _charsetManager.AddChar(ch);
                }
            }
            else
            {
                // 新建模式：创建新字符集
                _charsetManager = CharsetManager.Create(text, name);
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
