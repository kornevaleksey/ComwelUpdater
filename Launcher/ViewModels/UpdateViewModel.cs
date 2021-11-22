using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.ViewModels
{
    public class UpdateViewModel : BindableBase
    {
        public UpdateViewModel()
        {
            UpdateGameButtonText = "Обновить";
        }

        private string _updateGameButtonText;
        public string UpdateGameButtonText
        {
            get => _updateGameButtonText;
            set => SetProperty(ref _updateGameButtonText, value);
        }
    }
}
