using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AdsSyncClientAvaloniaDemo.Converters
{
    /// <summary>
    /// This converter negates a boolean value.
    /// </summary>
    internal class IsFalseConverter : IValueConverter
    {
        /// <summary>
        /// Returns TRUE if the value is a boolean and the value is FALSE.
        /// </summary>
        public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return value is bool boolean && !boolean;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}