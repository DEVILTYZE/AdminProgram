using System;
using System.Globalization;
using System.Windows.Data;
using AdminProgram.Models;
using SecurityChannel;

namespace AdminProgram.Converters
{
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return "Неизвестно";

            return (HostStatus)value switch
            {
                HostStatus.Unknown => "Неизвестно",
                HostStatus.Loading => "Загрузка...",
                HostStatus.Off => "Выключен",
                _ => "Включён"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return HostStatus.Unknown;

            if (string.CompareOrdinal((string)value, "Включён") == 0)
                return HostStatus.On;
            
            if (string.CompareOrdinal((string)value, "Выключен") == 0)
                return HostStatus.Off;
            
            return string.CompareOrdinal((string)value, "Загрузка...") == 0 
                ? HostStatus.Loading 
                : HostStatus.Unknown;
        }
    }
}