using System;
using System.Globalization;
using System.Windows.Data;

namespace AdminProgram.Converters
{
    public abstract class BoolStringConverter : IValueConverter
    {
        private readonly string[] _strings = new string[2];

        protected BoolStringConverter(string str1, string str2)
        {
            _strings[0] = str1;
            _strings[1] = str2;
        }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                throw new NullReferenceException("Value is null");

            return (bool)value ? _strings[1] : _strings[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                throw new NullReferenceException("Value is null");

            if (string.CompareOrdinal((string)value, _strings[1]) == 0)
                return true;

            if (string.CompareOrdinal((string)value, _strings[0]) == 0)
                return false;

            throw new ArgumentException("Invalid value");
        }
    }

    public class ScanButtonTextConverter : BoolStringConverter
    {
        public ScanButtonTextConverter() : base("Сканировать", "Сканирование...") { }
    }
    
    public class RefreshButtonTextConverter : BoolStringConverter
    {
        public RefreshButtonTextConverter() : base("Обновить статусы", "Обновление...") { }
    }

    public class TransferButtonTextConverter : BoolStringConverter
    {
        public TransferButtonTextConverter() : base("Скопировать файлы", "Отменить копирование") { }
    }
}