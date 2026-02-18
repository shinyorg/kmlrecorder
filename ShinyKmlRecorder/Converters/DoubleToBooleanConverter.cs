using System.Globalization;

namespace ShinyKmlRecorder;

public class DoubleToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return Math.Abs(d) > 0d;

        if (value is float f)
            return Math.Abs(f) > 0f;

        if (value is decimal m)
            return Math.Abs(m) > 0m;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("DoubleToBooleanConverter is one-way only.");
}

