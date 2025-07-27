using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FontMaker.ViewModel
{
    public partial class LanguagesViewModel : ObservableObject
    {
        /// <summary>
        /// 全局单例实例
        /// </summary>
        public static LanguagesViewModel Instance { get; private set; }

        /// <summary>
        /// 支持的语言列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<LanguageInfo> _supportedLanguages;

        /// <summary>
        /// 选中的语言
        /// </summary>
        [ObservableProperty]
        private LanguageInfo selectedLanguage;

        // <summary>
        /// 语言改变事件，切换语言后调用
        /// </summary>
        public static event EventHandler<CultureInfo> LanguageChanged;

        /// <summary>
        /// 当前语言
        /// </summary>
        public static CultureInfo CurrentLanguage { get; private set; } = CultureInfo.CurrentUICulture;


        public LanguagesViewModel()
        {
            Instance = this; // 设置单例实例
            LoadSupportedLanguages();
            LoadDefaultLanguage();
        }

        /// <summary>
        /// 静态初始化方法，在应用启动时调用
        /// </summary>
        public static void Initialize()
        {
            if (Instance == null)
            {
                new LanguagesViewModel();
            }
        }


        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="cultureCode">语言代码，如：zh-CN, en-US</param>
        /// <exception cref="ArgumentException">不支持的语言代码</exception>
        /// <exception cref="ArgumentNullException">语言代码为空</exception>
        public static void ChangeLanguage(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
                throw new ArgumentNullException(nameof(cultureCode));

            try
            {
                var culture = new CultureInfo(cultureCode);
                ChangeLanguage(culture);
            }
            catch (CultureNotFoundException ex)
            {
                var errorMessage = string.Format(FontMaker.Resources.Lang.Languages.UnsupportedLanguageCode, cultureCode);
                throw new ArgumentException(errorMessage, ex);
            }
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="culture">文化信息</param>
        /// <exception cref="ArgumentNullException">文化信息为空</exception>
        public static void ChangeLanguage(CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));

            // 设置当前线程的文化
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // 如果是WPF应用，设置应用程序的文化
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Thread.CurrentCulture = culture;
                Application.Current.Dispatcher.Thread.CurrentUICulture = culture;
            }

            // 更新当前语言
            CurrentLanguage = culture;

            // 触发语言改变事件
            LanguageChanged?.Invoke(null, culture);
        }

        /// <summary>
        /// 切换到指定的语言信息
        /// </summary>
        /// <param name="languageInfo">语言信息</param>
        public static void ChangeLanguage(LanguageInfo languageInfo)
        {
            if (languageInfo == null)
                throw new ArgumentNullException(nameof(languageInfo));

            ChangeLanguage(languageInfo.Code);
        }

        /// <summary>
        /// 程序支持的语言代码列表，添加新的语言时需要更新这个列表！
        /// </summary>
        private static readonly string[] SupportedLanguageCodes = new string[]
        {
            "zh-CN",  // 简体中文
            "en-US",  // 英文
            "zh-TW"   // 繁体中文
        };

        /// <summary>
        /// 加载支持的语言列表
        /// </summary>
        private void LoadSupportedLanguages()
        {
            var languages = new List<LanguageInfo>();

            foreach (var codeString in SupportedLanguageCodes)
            {
                var culture = new CultureInfo(codeString);
                var languageInfo = new LanguageInfo
                {
                    Code = culture.Name,
                    DisplayName = culture.DisplayName,
                    NativeName = culture.NativeName,
                    IsDefault = culture.Equals(CultureInfo.CurrentUICulture)
                };
                languages.Add(languageInfo);
            }

            if (!languages.Any())
            {
                throw new InvalidOperationException(FontMaker.Resources.Lang.Languages.ErrorNoValidLanguageResourcesParsed);
            }

            // 设置支持的语言列表
            SupportedLanguages = new ObservableCollection<LanguageInfo>(languages.OrderBy(l => l.DisplayName));
        }

        partial void OnSelectedLanguageChanged(LanguageInfo value)
        {
            if (value != null)
            {
                ChangeLanguage(value);
                SaveDefaultLanguage(value);
            }
        }


        /// <summary>
        /// 加载默认语言设置
        /// </summary>
        private void LoadDefaultLanguage()
        {
            LanguageInfo selectedLanguage = null;

            try
            {
                var defaultLanguageCode = FontMaker.Data.Config.DefaultLanguageCode;
                if (!string.IsNullOrEmpty(defaultLanguageCode))
                {
                    selectedLanguage = SupportedLanguages?.FirstOrDefault(l => l.Code == defaultLanguageCode);
                }

                if (selectedLanguage == null)
                {
                    var currentCultureCode = CurrentLanguage.Name;
                    selectedLanguage = SupportedLanguages?.FirstOrDefault(l => l.Code == currentCultureCode);
                }

                if (selectedLanguage == null)
                {
                    selectedLanguage = SupportedLanguages?.FirstOrDefault(l => l.Code == "en-US");
                }

                if (selectedLanguage == null && SupportedLanguages?.Any() == true)
                {
                    selectedLanguage = SupportedLanguages.First();
                }

                // 应用选择的语言
                if (selectedLanguage != null)
                {
                    SelectedLanguage = selectedLanguage;
                    ChangeLanguage(selectedLanguage);
                    
                    // 如果这是从系统语言或英文回退来的，保存为新的默认语言
                    if (string.IsNullOrEmpty(FontMaker.Data.Config.DefaultLanguageCode))
                    {
                        SaveDefaultLanguage(selectedLanguage);
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常情况下的最后回退
                if (SupportedLanguages?.Any() == true)
                {
                    SelectedLanguage = SupportedLanguages.First();
                }
            }
        }

        /// <summary>
        /// 保存默认语言设置
        /// </summary>
        private void SaveDefaultLanguage(LanguageInfo languageInfo)
        {
            try
            {
                FontMaker.Data.Config.DefaultLanguageCode = languageInfo.Code;
            }
            catch (Exception)
            {
                // 静默处理保存失败
            }
        }
    }


    /// <summary>
    /// 语言信息类
    /// </summary>
    public class LanguageInfo
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string NativeName { get; set; }
        public bool IsDefault { get; set; }

        public override string ToString()
        {
            return NativeName;
        }

        public override bool Equals(object obj)
        {
            return obj is LanguageInfo other && Code == other.Code;
        }

        public override int GetHashCode()
        {
            return Code?.GetHashCode() ?? 0;
        }
    }
}
 