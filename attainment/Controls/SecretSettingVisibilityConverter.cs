using System.Globalization;
using System.Windows;
using System.Windows.Data;
using attainment.Models;

namespace attainment.Controls;

public sealed class SecretSettingVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isSecret = string.Equals(value as string, SettingKeys.OpenAIKey, StringComparison.Ordinal);
        var showSecret = string.Equals(parameter as string, "Secret", StringComparison.OrdinalIgnoreCase);
        return isSecret == showSecret ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
