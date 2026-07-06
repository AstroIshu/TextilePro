using System;
using System.Globalization;
using System.Windows.Data;

namespace TextilePro.UI.Converters;

public class ScoreToClassTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int score)
        {
            if (score >= 10 && score <= 24) return "A";
            if (score >= 5 && score <= 9) return "B";
            return "C";
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}