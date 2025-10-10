using System;
using System.Globalization;
using Xamarin.Forms;

namespace DiagnosticNP.Converters
{
    public class PollingStateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPolling)
            {
                return isPolling ? "ОПРОС АКТИВЕН" : "ОПРОС ОСТАНОВЛЕН";
            }
            return "НЕИЗВЕСТНО";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}