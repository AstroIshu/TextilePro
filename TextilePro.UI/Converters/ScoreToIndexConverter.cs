using System;
using System.Globalization;
using System.Windows.Data;

namespace TextilePro.UI.Converters;

public class ScoreToIndexConverter : IValueConverter
{
    // Converts score (0,1,2) to combo index (2,1,0)
    // Because the options are ordered: [2 pts, 1 pt, 0 pt]
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int score && score >= 0 && score <= 2)
            return 2 - score; // 2->0, 1->1, 0->2
        return -1; // Not selected
    }

    // Converts combo index (0,1,2) back to score (2,1,0)
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0 && index <= 2)
            return 2 - index; // 0->2, 1->1, 2->0
        return -1;
    }
}