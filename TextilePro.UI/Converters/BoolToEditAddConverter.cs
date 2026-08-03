using System;
using System.Globalization;
using System.Windows.Data;

namespace TextilePro.UI.Converters;

public class BoolToEditAddConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isEdit && isEdit
            ? "✏️ Edit Inventory Entry"
            : "➕ Manual Inventory Entry";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
