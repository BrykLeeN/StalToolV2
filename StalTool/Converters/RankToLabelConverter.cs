using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StalTool.Converters;

public sealed class RankToLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rank = (value as string)?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(rank))
            return string.Empty;

        return rank switch
        {
            "common" => "Новичок",
            "rare" => "Сталкер",
            "epic" => "Ветеран",
            "master" => "Мастер",
            "legendary" => "Легенда",
            "artifact_common" => "Обычный",
            "artifact_uncommon" => "Необычный",
            "artifact_special" => "Особый",
            "artifact_rare" => "Редкий",
            "artifact_exceptional" => "Исключительный",
            "artifact_legendary" => "Легендарный",
            _ => rank
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
