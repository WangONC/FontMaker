using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace FontMaker.Data.Models
{
    /// <summary>
    /// 字符集管理器
    /// </summary>
    public class CharsetManager
    {
        private List<char> _characters;
        private string _name;
        private string _filePath;

        public CharsetManager()
        {
            _characters = new List<char>();
            _name = string.Empty;
            _filePath = string.Empty;
        }

        /// <summary>
        /// 字符集名称
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 字符集文件路径
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// 字符总数
        /// </summary>
        public int CharCount => _characters.Count;

        /// <summary>
        /// 所有字符的只读集合
        /// </summary>
        public IReadOnlyList<char> Characters => _characters.AsReadOnly();

        /// <summary>
        /// 根据索引获取字符
        /// </summary>
        /// <param name="index">字符索引</param>
        /// <returns>指定索引的字符</returns>
        public char GetChar(int index)
        {
            if (index < 0 || index >= _characters.Count)
                throw new IndexOutOfRangeException($"字符索引超出范围: {index}");
            
            return _characters[index];
        }

        public int GetCharCount()
        {
            return _characters.Count;
        }

        /// <summary>
        /// 获取字符在字符集中的索引
        /// </summary>
        /// <param name="ch">要查找的字符</param>
        /// <returns>字符索引，如果不存在返回-1</returns>
        public int GetCharIndex(char ch)
        {
            return _characters.IndexOf(ch);
        }

        /// <summary>
        /// 检查字符是否存在于字符集中
        /// </summary>
        /// <param name="ch">要检查的字符</param>
        /// <returns>是否存在</returns>
        public bool ContainsChar(char ch)
        {
            return _characters.Contains(ch);
        }

        /// <summary>
        /// 从cst文件加载字符集
        /// </summary>
        /// <param name="filePath">cst文件路径</param>
        /// <returns>加载的字符集管理器，失败返回null</returns>
        public static CharsetManager LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                // 读取二进制文件内容
                byte[] fileBytes = File.ReadAllBytes(filePath);
                
                // 文件长度必须是2的倍数（每个字符2字节）
                if (fileBytes.Length % 2 != 0)
                {
                    return null;
                }

                // 解析UTF-16 LE编码的字符
                var uniqueChars = new LinkedHashSet<char>();
                for (int i = 0; i < fileBytes.Length; i += 2)
                {
                    // UTF-16 LE: 低字节在前，高字节在后
                    char ch = (char)(fileBytes[i] | (fileBytes[i + 1] << 8));
                    uniqueChars.Add(ch);
                }

                if (uniqueChars.Count == 0)
                {
                    return null;
                }

                var charset = new CharsetManager();
                charset._filePath = filePath;
                charset._name = Path.GetFileNameWithoutExtension(filePath);
                charset._characters.AddRange(uniqueChars);
                
                return charset;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 从字符串创建字符集（用户自定义）
        /// </summary>
        /// <param name="characters">字符串</param>
        /// <param name="name">字符集名称</param>
        /// <returns>创建的字符集管理器</returns>
        public static CharsetManager Create(string characters, string name = "用户自定义")
        {
            var charset = new CharsetManager();
            charset._name = name;
            charset._filePath = string.Empty;

            if (!string.IsNullOrEmpty(characters))
            {
                // 去重并保持顺序
                var uniqueChars = new LinkedHashSet<char>();
                foreach (char ch in characters)
                {
                    uniqueChars.Add(ch);
                }
                charset._characters.AddRange(uniqueChars);
            }

            return charset;
        }

        /// <summary>
        /// 从字符数组创建字符集
        /// </summary>
        /// <param name="characters">字符数组</param>
        /// <param name="name">字符集名称</param>
        /// <returns>创建的字符集管理器</returns>
        public static CharsetManager Create(char[] characters, string name = "用户自定义")
        {
            if (characters != null)
            {
                return Create(new string(characters), name);
            }
            return Create(string.Empty, name);
        }

        /// <summary>
        /// 保存字符集到cst文件
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <returns>保存是否成功</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                // 将字符转换为UTF-16 LE字节数组
                var bytes = new List<byte>();
                foreach (char ch in _characters)
                {
                    // UTF-16 LE: 低字节在前，高字节在后
                    ushort unicode = (ushort)ch;
                    bytes.Add((byte)(unicode & 0xFF));        // 低字节
                    bytes.Add((byte)((unicode >> 8) & 0xFF)); // 高字节
                }

                File.WriteAllBytes(filePath, bytes.ToArray());
                _filePath = filePath;
                _name = Path.GetFileNameWithoutExtension(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 添加字符到字符集
        /// </summary>
        /// <param name="ch">要添加的字符</param>
        public void AddChar(char ch)
        {
            if (!_characters.Contains(ch))
            {
                _characters.Add(ch);
            }
        }

        /// <summary>
        /// 从字符集中移除字符
        /// </summary>
        /// <param name="ch">要移除的字符</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveChar(char ch)
        {
            return _characters.Remove(ch);
        }

        /// <summary>
        /// 清空字符集
        /// </summary>
        public void Delete()
        {
            _characters.Clear();
            _name = string.Empty;
            _filePath = string.Empty;
        }

        /// <summary>
        /// 获取字符的Unicode编码信息
        /// </summary>
        /// <param name="ch">字符</param>
        /// <returns>Unicode信息字符串</returns>
        public static string GetCharInfo(char ch)
        {
            int unicode = (int)ch;
            return $"U+{unicode:X4}";
        }

        /// <summary>
        /// 获取指定索引字符的详细信息
        /// </summary>
        /// <param name="index">字符索引</param>
        /// <returns>字符信息字符串，格式：U+xxxx (index/total)</returns>
        public string GetCharInfo(int index)
        {
            if (index < 0 || index >= _characters.Count)
                return "无效索引";

            char ch = _characters[index];
            int unicode = (int)ch;
            return $"U+{unicode:X4} ({index + 1}/{_characters.Count})";
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// 保持插入顺序的HashSet实现
    /// </summary>
    public class LinkedHashSet<T> : ICollection<T>
    {
        private readonly List<T> _list = new List<T>();
        private readonly HashSet<T> _set = new HashSet<T>();

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            if (_set.Add(item))
            {
                _list.Add(item);
                return true;
            }
            return false;
        }

        void ICollection<T>.Add(T item) => Add(item);

        public bool Remove(T item)
        {
            if (_set.Remove(item))
            {
                _list.Remove(item);
                return true;
            }
            return false;
        }

        public bool Contains(T item) => _set.Contains(item);
        public void Clear()
        {
            _list.Clear();
            _set.Clear();
        }

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}