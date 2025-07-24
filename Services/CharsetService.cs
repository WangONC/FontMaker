using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FontMaker.Data.Models
{
    /// <summary>
    /// 字符集服务
    /// </summary>
    public class CharsetService
    {
        private readonly ObservableCollection<CharsetInfo> _availableCharsetsInfo;
        private readonly ObservableCollection<CharsetManager> _availableCharsets;
        private readonly string _charsetDirectory;

        public CharsetService()
        {
            _availableCharsetsInfo = new ObservableCollection<CharsetInfo>();
            _availableCharsets = new ObservableCollection<CharsetManager>();

            // 获取字符集目录路径
            _charsetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "CharSets");
            
            // 如果目录不存在，尝试其他可能的路径
            if (!Directory.Exists(_charsetDirectory))
            {
                _charsetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "charset");
            }
        }

        /// <summary>
        /// 可用字符集列表
        /// </summary>
        public ObservableCollection<CharsetInfo> AvailableCharsetsInfo => _availableCharsetsInfo;
        public ObservableCollection<CharsetManager> AvailableCharsets => _availableCharsets;

        /// <summary>
        /// 字符集目录路径
        /// </summary>
        public string CharsetDirectory => _charsetDirectory;

        /// <summary>
        /// 初始化字符集列表（扫描cst文件）
        /// </summary>
        /// <returns>找到的字符集数量</returns>
        public int InitializeCharsets()
        {
            _availableCharsetsInfo.Clear();

            if (!Directory.Exists(_charsetDirectory))
            {
                return 0;
            }

            try
            {
                // 搜索所有.cst文件
                var cstFiles = Directory.GetFiles(_charsetDirectory, "*.cst", SearchOption.TopDirectoryOnly);
                
                foreach (var filePath in cstFiles.OrderBy(f => f))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var info = new CharsetInfo
                    {
                        Name = fileName,
                        FilePath = filePath,
                        IsBuiltIn = true
                    };
                    
                    _availableCharsetsInfo.Add(info);
                    // 预加载字符集管理器
                    var charsetManager = CharsetManager.LoadFromFile(filePath);
                    if (charsetManager != null)
                    {
                        _availableCharsets.Add(charsetManager);
                    }
                }

                return _availableCharsetsInfo.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 根据名称获取字符集信息
        /// </summary>
        /// <param name="name">字符集名称</param>
        /// <returns>字符集信息，如果未找到返回null</returns>
        public CharsetInfo? GetCharsetInfo(string name)
        {
            return _availableCharsetsInfo.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public CharsetManager? GetCharset(string name)
        {
            return _availableCharsets.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 添加用户自定义字符集
        /// </summary>
        /// <param name="name">字符集名称</param>
        /// <param name="characters">字符内容</param>
        /// <param name="filePath">文件路径（可选）</param>
        /// <returns>是否添加成功</returns>
        public bool AddCustomCharset(string name, string characters, out CharsetManager? ocharset, string filePath = null)
        {
            // 检查名称是否已存在
            if (_availableCharsetsInfo.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ocharset = null;
                return false;
            }

            var info = new CharsetInfo
            {
                Name = name,
                FilePath = filePath ?? string.Empty,
                IsBuiltIn = false
            };

            _availableCharsetsInfo.Add(info);
            var charset = CharsetManager.Create(characters, name);
            if (charset == null)
            {
                ocharset = null;
                return false;
            }
            _availableCharsets.Add(charset);
            if (filePath != null)
            {
                charset.SaveToFile(info.FilePath);
            }
            ocharset = charset;
            return true;
        }

        /// <summary>
        /// 移除字符集
        /// </summary>
        /// <param name="name">字符集名称</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveCharset(string name)
        {
            var info = GetCharsetInfo(name);
            var charset = GetCharset(name);
            if (info != null && charset != null && !info.IsBuiltIn)
            {
                return _availableCharsetsInfo.Remove(info) && _availableCharsets.Remove(charset);
            }
            return false;
        }

        /// <summary>
        /// 导入字符集文件
        /// </summary>
        /// <param name="filePath">字符集文件路径</param>
        /// <returns>导入是否成功</returns>
        public bool ImportCharsetFile(string filePath,out CharsetManager? charset)
        {
            if (!File.Exists(filePath))
            { 
                charset = null;
                return false;
            }

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // 检查是否已存在
                if (_availableCharsetsInfo.Any(c => c.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    charset = null;
                    return false;
                }

                // 验证文件是否为有效的字符集文件
                var testCharset = CharsetManager.LoadFromFile(filePath);
                if (testCharset == null)
                {
                    charset = null;
                    return false;
                }

                // 复制文件到字符集目录
                var targetPath = Path.Combine(_charsetDirectory, Path.GetFileName(filePath));
                if (!filePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO 在拥有配置文件机制之前，不做持久化
                    //File.Copy(filePath, targetPath, true);
                }

                var info = new CharsetInfo
                {
                    Name = fileName,
                    FilePath = targetPath,
                    IsBuiltIn = false
                };

                _availableCharsetsInfo.Add(info);
                _availableCharsets.Add(testCharset);
                charset = testCharset;
                return true;
            }
            catch (Exception)
            {
                charset = null;
                return false;
            }
        }

        /// <summary>
        /// 获取字符集统计信息
        /// </summary>
        /// <param name="name">字符集名称</param>
        /// <returns>统计信息</returns>
        public CharsetStatistics? GetCharsetStatistics(string name)
        {
            var charset = GetCharset(name);
            if (charset == null)
                return null;

            var stats = new CharsetStatistics
            {
                Name = charset.Name,
                TotalChars = charset.CharCount,
                FilePath = charset.FilePath
            };

            // 统计字符类型
            foreach (var ch in charset.Characters)
            {
                if (char.IsLetter(ch))
                    stats.LetterCount++;
                else if (char.IsDigit(ch))
                    stats.DigitCount++;
                else if (char.IsPunctuation(ch) || char.IsSymbol(ch))
                    stats.SymbolCount++;
                else if (char.IsWhiteSpace(ch))
                    stats.WhitespaceCount++;
                else
                    stats.OtherCount++;
            }

            return stats;
        }
    }

    /// <summary>
    /// 字符集信息
    /// </summary>
    public class CharsetInfo
    {
        /// <summary>
        /// 字符集名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 是否为内置字符集
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => Name;

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 字符集统计信息
    /// </summary>
    public class CharsetStatistics
    {
        /// <summary>
        /// 字符集名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 总字符数
        /// </summary>
        public int TotalChars { get; set; }

        /// <summary>
        /// 字母数量
        /// </summary>
        public int LetterCount { get; set; }

        /// <summary>
        /// 数字数量
        /// </summary>
        public int DigitCount { get; set; }

        /// <summary>
        /// 符号数量
        /// </summary>
        public int SymbolCount { get; set; }

        /// <summary>
        /// 空白字符数量
        /// </summary>
        public int WhitespaceCount { get; set; }

        /// <summary>
        /// 其他字符数量
        /// </summary>
        public int OtherCount { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
    }
}