using CommunityToolkit.Mvvm.ComponentModel;
using FontMaker.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontMaker.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        public FontComboBoxViewModel FontVM { get; }
        public CharsetViewModel CharsetVM { get; }
        public ExportViewModel ExportVM { get; }

        public MainViewModel()
        {
            FontVM = new FontComboBoxViewModel();
            CharsetVM = new CharsetViewModel();
            ExportVM = new ExportViewModel();
        }
    }


}
