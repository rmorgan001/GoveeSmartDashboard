using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace GoVeeSmartDashboard.Views;

public sealed class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance=new();
    public object Convert(object? value,Type targetType,object? parameter,CultureInfo culture) =>
        new SolidColorBrush(Color.Parse(value is true ? "#39D353" : "#F85149"));
    public object ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture) =>
        throw new NotSupportedException();
}
