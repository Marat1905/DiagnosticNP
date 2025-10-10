using System;
using System.Globalization;
using Xamarin.Forms;

namespace DiagnosticNP.Converters
{
    public class IsMeasurementPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name)
            {
                return name == "Горизонтальная" ||
                       name == "Вертикальная" ||
                       name == "Осевая";
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}