using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AdminProgram.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not null && (bool)value ? Visibility.Hidden : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not null && (Visibility)value == Visibility.Hidden;
    }
}