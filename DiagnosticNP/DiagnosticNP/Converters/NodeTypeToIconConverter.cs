using System;
using System.Globalization;
using Xamarin.Forms;
using DiagnosticNP.Models.Equipment;

namespace DiagnosticNP.Converters
{
    public class NodeTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeType nodeType)
            {
                switch (nodeType)
                {
                    case NodeType.Equipment:
                        return "🏭";
                    case NodeType.Section:
                        return "📦";
                    case NodeType.Component:
                        return "⚙️";
                    case NodeType.MeasurementPoint:
                        return "📍";
                    case NodeType.Direction:
                        return "📐";
                    default:
                        return "📄";
                }
            }
            return "📄";
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
            if (value is NodeType nodeType)
            {
                switch (nodeType)
                {
                    case NodeType.Equipment:
                        return Color.FromHex("#4A6572");
                    case NodeType.Section:
                        return Color.FromHex("#344955");
                    case NodeType.Component:
                        return Color.FromHex("#F9AA33");
                    case NodeType.MeasurementPoint:
                        return Color.FromHex("#232F34");
                    case NodeType.Direction:
                        return Color.FromHex("#4CAF50");
                    default:
                        return Color.FromHex("#666666");
                }
            }
            return Color.FromHex("#666666");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return new Thickness(8 + (level * 12), 4, 8, 4);
            }
            return new Thickness(8, 4, 8, 4);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SelectedToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue && boolValue) ? Color.FromHex("#E3F2FD") : Color.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SelectedToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue && boolValue) ? Color.FromHex("#1976D2") : Color.FromHex("#212121");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Простые конвертеры для кнопок
    public class BoolToBackgroundColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Color.FromHex("#2196F3");
        public Color FalseColor { get; set; } = Color.FromHex("#E0E0E0");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue && boolValue) ? TrueColor : FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Color.White;
        public Color FalseColor { get; set; } = Color.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue && boolValue) ? TrueColor : FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : value;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter as string == "inverse";

            if (value is int count)
            {
                var result = count > 0;
                return inverse ? !result : result;
            }

            if (value is System.Collections.ICollection collection)
            {
                var result = collection.Count > 0;
                return inverse ? !result : result;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}