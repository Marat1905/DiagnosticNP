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

            if (status == "Подключено" || status == "Подключение...")
                return Color.FromHex("#27AE60"); // Success green
            else if (status == "Не подключено")
                return Color.FromHex("#95A5A6"); // Gray
            else if (status == "Ошибка подключения")
                return Color.FromHex("#E74C3C"); // Error red
            else
                return Color.FromHex("#95A5A6"); // Default gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}