using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TextilePro.UI.Converters;

public class RiskToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string risk)
        {
            return risk.ToLower() switch
            {
                "virgin" => new SolidColorBrush(Color.FromRgb(46, 125, 50)),      // Green
                "non-virgin" => new SolidColorBrush(Color.FromRgb(237, 108, 2)),   // Orange
                "virgin and non-virgin" => new SolidColorBrush(Color.FromRgb(2, 136, 209)), // Blue
                "not listed" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}