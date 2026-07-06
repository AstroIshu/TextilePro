using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TextilePro.UI.Converters;

public class ClassToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string cls)
        {
            return cls.ToUpper() switch
            {
                "A" => new SolidColorBrush(Color.FromRgb(46, 125, 50)),   // Green
                "B" => new SolidColorBrush(Color.FromRgb(237, 108, 2)),   // Orange
                "C" => new SolidColorBrush(Color.FromRgb(211, 47, 47)),   // Red
                "PENDING" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}