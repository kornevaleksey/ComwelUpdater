using System;
using System.Collections.Generic;
using System.Text;

namespace Updater.Models
{
    public class SizeConverter
    {
        public static string Convert(double size)
        {
            if (size < 0)
            {
                return "!";
            } else if (size < 1024)
            {
                return $"{size} Б";
            } else if (size < 1024 *1024)
            {
                return $"{size/1024 :F2} КиБ";
            } else if (size < 1024 *1024 *1024)
            {
                return $"{size / 1024 / 1024 :F2} МиБ";
            } else
            {
                return $"{size / 1024 / 1024 / 1024:F2} ГиБ";
            }
        }
    }
}
