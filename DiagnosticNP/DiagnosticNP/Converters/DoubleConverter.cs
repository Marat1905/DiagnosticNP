using System;
using System.Globalization;
using Xamarin.Forms;

namespace DiagnosticNP.Converters
{
    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Преобразуем double в строку с инвариантной культурой (использует точку)
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // Удаляем лишние пробелы
                stringValue = stringValue.Trim();

                if (string.IsNullOrEmpty(stringValue))
                    return 0.0;

                // Сначала пробуем распарсить с инвариантной культурой
                if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }

                // Если не получилось, заменяем запятую на точку и пробуем снова
                stringValue = stringValue.Replace(',', '.');
                if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }

                // Если все еще не получается, пробуем с текущей культурой
                if (double.TryParse(stringValue, NumberStyles.Any, culture, out result))
                {
                    return result;
                }
            }
            return 0.0;
        }
    }
}