using System;
using System.Globalization;
using Xamarin.Forms;

namespace DiagnosticNP.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString() ?? "";

            if (status == "Подключено")
                return Color.LightGreen;
            else if (status == "Не подключено")
                return Color.LightGray;
            else if (status == "Ошибка подключения")
                return Color.LightPink;
            else
                return Color.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}