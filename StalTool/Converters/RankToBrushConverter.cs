using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StalTool.Converters;

public sealed class RankToBrushConverter : IValueConverter
{
    private static readonly IBrush DefaultText = new SolidColorBrush(Color.Parse("#D6D0ED"));
    private static readonly IBrush DefaultBorder = new SolidColorBrush(Color.Parse("#5A3D99"));
    private static readonly IBrush DefaultBackground = new SolidColorBrush(Color.Parse("#261B42"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rank = (value as string)?.Trim().ToLowerInvariant();
        var mode = (parameter as string)?.Trim().ToLowerInvariant();

        var (text, border, background) = rank switch
        {
            "common" => ("#58D26A", "#2F8F49", "#1A2F20"),
            "rare" => ("#58A6FF", "#2F5FA8", "#1A213A"),
            "epic" => ("#FF6EC7", "#A84A84", "#331A2E"),
            "master" => ("#FF5C5C", "#B53A3A", "#381C1C"),
            "legendary" => ("#FFD45A", "#B08A2E", "#3A2F16"),
            "artifact_common" => ("#C5C7CF", "#7A8094", "#222632"),
            "artifact_uncommon" => ("#58D26A", "#2F8F49", "#1A2F20"),
            "artifact_special" => ("#58A6FF", "#2F5FA8", "#1A213A"),
            "artifact_rare" => ("#FF6EC7", "#A84A84", "#331A2E"),
            "artifact_exceptional" => ("#FF5C5C", "#B53A3A", "#381C1C"),
            "artifact_legendary" => ("#FFD45A", "#B08A2E", "#3A2F16"),
            _ => (null, null, null)
        };

        return mode switch
        {
            "background" => background is not null ? new SolidColorBrush(Color.Parse(background)) : DefaultBackground,
            "border" => border is not null ? new SolidColorBrush(Color.Parse(border)) : DefaultBorder,
            _ => text is not null ? new SolidColorBrush(Color.Parse(text)) : DefaultText
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
