using System.Globalization;
using System;
using System.Windows.Data;

namespace TextilePro.UI.Converters;

public class DecimalToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString("F2");
        return "0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}