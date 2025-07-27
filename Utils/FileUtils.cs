using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontMaker.Utils
{
    class FileUtils
    {
        /// <summary>
        /// 获取指定目录下的所有文件，调用时外部应当包围在try-catch中以处理可能的异常，函数内部不处理异常。
        /// </summary>
        /// <param name="path">要搜索的目录路径</param>
        /// <param name="recursive">是否递归搜索子目录</param>
        /// <returns>文件路径列表</returns>
        public static List<string> GetAllFiles(string path, bool recursive)
        {
            // 检查目录是否存在
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(string.Format(FontMaker.Resources.Lang.Languages.DirectoryNotFound, path));
            }

            // 创建结果列表
            List<string> files = new();

            // 获取当前目录中的文件
            files.AddRange(Directory.GetFiles(path));

            // 如果需要递归搜索
            if (recursive)
            {
                // 获取所有子目录
                foreach (string subDir in Directory.GetDirectories(path))
                {
                    try
                    {
                        // 递归获取子目录中的文件
                        files.AddRange(GetAllFiles(subDir, true));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 忽略没有访问权限的目录
                        continue;
                    }
                }
            }

            return files;
        }

        // <summary>
        // 创建文件，调用时外部应当包围在try-catch中以处理可能的异常，函数内部不处理异常。
        // </summary>
        public static bool CreateFile(string filePath, string content)
        {
            // 确保目录存在
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                return false;
            }
            // 写入内容到文件
            File.WriteAllText(filePath, content);
            return true;
        }

        public static bool CreateFile(string filePath, byte[] content)
        {
            // 确保目录存在
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                return false;
            }
            // 写入内容到文件
            File.WriteAllBytes(filePath, content);
            return true;
        }

        // 读取文件内容
        public static string ReadFile(string filePath)
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(string.Format(FontMaker.Resources.Lang.Languages.FileNotFound, filePath));
            }
            // 读取文件内容
            return File.ReadAllText(filePath);
        }

        public static byte[] ReadFileBytes(string filePath)
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(string.Format(FontMaker.Resources.Lang.Languages.FileNotFound, filePath));
            }
            // 读取文件内容
            return File.ReadAllBytes(filePath);
        }


        /// <summary>
        /// 获取保存文件路径
        /// </summary>
        public static string GetSaveFilePath(string filter, string defaultFileName)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName,
                DefaultExt = System.IO.Path.GetExtension(defaultFileName)
            };

            return saveDialog.ShowDialog() == true ? saveDialog.FileName : string.Empty;
        }
    }
}
