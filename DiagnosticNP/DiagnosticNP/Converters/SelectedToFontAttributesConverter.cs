using System;
using System.Globalization;
using Xamarin.Forms;
using DiagnosticNP.Models.Equipment;

namespace DiagnosticNP.Converters
{
    public class SelectedToFontAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? FontAttributes.Bold : FontAttributes.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DirectionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is NodeType nodeType && nodeType == NodeType.Direction;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var colors = (parameter as string)?.Split('|');
            var trueColor = colors?.Length > 0 ? colors[0] : "Gray";
            var falseColor = colors?.Length > 1 ? colors[1] : "White";

            return (bool)value ? Color.FromHex(trueColor) : Color.FromHex(falseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}