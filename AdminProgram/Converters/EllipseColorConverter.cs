using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AdminProgram.Models;
using CommandLib;
using SecurityChannel;

namespace AdminProgram.Converters
{
    public class EllipseColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return new SolidColorBrush(Colors.Gray);

            return (HostStatus)value switch
            {
                HostStatus.Unknown => new SolidColorBrush(Colors.Gray),
                HostStatus.Loading => new SolidColorBrush(Colors.Orange),
                HostStatus.Off => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Green)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return HostStatus.Unknown;
            
            if (((SolidColorBrush)value).Color == Colors.Green)
                return HostStatus.On;
            
            if (((SolidColorBrush)value).Color == Colors.Red)
                return HostStatus.Off;
            
            return ((SolidColorBrush)value).Color == Colors.Orange 
                ? HostStatus.Loading 
                : HostStatus.Unknown;
        }
    }
}