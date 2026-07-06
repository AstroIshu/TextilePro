using System;
using System.Globalization;
using System.Windows.Data;

namespace TextilePro.UI.Converters;

public class ClassToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string cls)
        {
            return cls.ToUpper() switch
            {
                "A" => "Class A",
                "B" => "Class B",
                "C" => "Class C",
                "PENDING" => "Not Evaluated",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}