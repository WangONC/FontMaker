using CommunityToolkit.Mvvm.ComponentModel;
using FontMaker.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontMaker.ViewModel
{
    public partial class CharsetViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CharsetManager>? _defaultCharsets;
        [ObservableProperty]
        private ObservableCollection<CharsetManager>? _allCharsets;
        [ObservableProperty]
        private ObservableCollection<CharsetManager>? _importedCharsets;
        [ObservableProperty]
        private CharsetManager? _selectedCharset;

        private CharsetService charsetService= new CharsetService();

        public CharsetViewModel()
        {
            ImportedCharsets = new ObservableCollection<CharsetManager>();
            AllCharsets = new ObservableCollection<CharsetManager>();
            DefaultCharsets = new ObservableCollection<CharsetManager>();

            charsetService.InitializeCharsets();

            foreach (var charset in charsetService.AvailableCharsets)
            {
                DefaultCharsets.Add(charset);
                AllCharsets.Add(charset);
            }
            SelectedCharset = DefaultCharsets.FirstOrDefault();
        }

        public bool AddImportedCharset(string charsetPath)
        {
            CharsetManager? charset;
            if (charsetService.ImportCharsetFile(charsetPath, out charset))
            {
                if (charset != null)
                {
                    ImportedCharsets?.Add(charset);
                    AllCharsets?.Add(charset);
                    SelectedCharset = charset;
                    return true;
                }
                return false;
            }
            return false;

        }
    }
}
