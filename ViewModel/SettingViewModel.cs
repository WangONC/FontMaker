using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontMaker.ViewModel
{
    public partial class SettingViewModel : ObservableObject
    {
        public ExportViewModel ExportVM { get; }
        public LanguagesViewModel LanguagesViewModel { get; }

        public SettingViewModel()
        {
            ExportVM = new ExportViewModel();
            LanguagesViewModel = FontMaker.ViewModel.LanguagesViewModel.Instance ?? new LanguagesViewModel();
        }
    }
}
