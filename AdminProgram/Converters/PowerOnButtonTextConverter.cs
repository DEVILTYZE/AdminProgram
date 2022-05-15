using System;
using System.Globalization;
using System.Windows.Data;
using AdminProgram.Models;
using SecurityChannel;

namespace AdminProgram.Converters
{
    public class PowerOnButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return "Недоступно";

            return (HostStatus)value switch
            {
                HostStatus.Unknown => "Недоступно",
                HostStatus.Loading => "Загрузка...",
                HostStatus.Off => "Включить",
                _ => "Выключить"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return HostStatus.Unknown;

            if (string.CompareOrdinal((string)value, "Выключить") == 0)
                return HostStatus.On;
            
            if (string.CompareOrdinal((string)value, "Включить") == 0)
                return HostStatus.Off;
            
            return string.CompareOrdinal((string)value, "Загрузка...") == 0 
                ? HostStatus.Loading 
                : HostStatus.Unknown;
        }
    }
}