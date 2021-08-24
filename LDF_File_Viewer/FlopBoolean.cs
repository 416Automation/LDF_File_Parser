using System;
using System.Windows.Data;

namespace LDF_File_Viewer
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class FlopBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var test = (bool)value;
            if (test) return "1";
            return "0";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            return true;
        }
    }
}
