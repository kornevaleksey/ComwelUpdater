using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.DesignModels
{
    internal class DesignUpdateViewModel : BindableBase
    {
        public string InfoBlock { get; set; } = "Информация информация";
        public string InfoBlockAdd { get; set; } = "Дополнительная информация";
        public string UpdateGameButtonText { get; set; } = "Обновить";
        public double OverallProgress { get; set; } = 20;
        public double MinProgress { get; set; } = 0;
        public double MaxProgress { set; get; } = 100;
    }
}
