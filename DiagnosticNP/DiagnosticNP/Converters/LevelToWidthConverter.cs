using System;
using System.Globalization;
using Xamarin.Forms;

namespace DiagnosticNP.Converters
{
    public class LevelToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return new GridLength(level * 20, GridUnitType.Absolute);
            }
            return new GridLength(0, GridUnitType.Absolute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ExpandedToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "▼" : "►";
            }
            return "►";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return FontAttributes.Bold;
            }
            return FontAttributes.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                // Градиент от синего к фиолетовому в зависимости от уровня
                switch (level)
                {
                    case 0:
                        return Color.FromHex("#3498DB");  // Синий
                    case 1:
                        return Color.FromHex("#2980B9");  // Темно-синий
                    case 2:
                        return Color.FromHex("#8E44AD");  // Фиолетовый
                    case 3:
                        return Color.FromHex("#9B59B6");  // Светло-фиолетовый
                    case 4:
                        return Color.FromHex("#E74C3C");  // Красный для точек замера
                    default:
                        return Color.FromHex("#95A5A6");   // Серый по умолчанию
                }
            }
            return Color.FromHex("#3498DB");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NodeTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMeasurementPoint)
            {
                return isMeasurementPoint ? Color.FromHex("#E74C3C") : Color.FromHex("#3498DB");
            }
            return Color.FromHex("#3498DB");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NodeTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMeasurementPoint)
            {
                return isMeasurementPoint ? "📊" : "📁";
            }
            return "📁";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.3;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}