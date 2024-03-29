﻿using System;
using System.Globalization;
using System.Windows.Data;
using CommandLib;

namespace AdminProgram.Converters
{
    public class StatusRefreshBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return false;

            return (HostStatus)value switch
            {
                HostStatus.Unknown => true,
                HostStatus.Loading => false,
                HostStatus.Off => true,
                _ => true
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return HostStatus.Unknown;

            return (bool)value ? HostStatus.On : HostStatus.Unknown;
        }
    }
}