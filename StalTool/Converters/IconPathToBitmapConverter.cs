using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace StalTool.Converters;

public sealed class IconPathToBitmapConverter : IValueConverter
{
    public static readonly IconPathToBitmapConverter Instance = new();
    private readonly ConcurrentDictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string raw || string.IsNullOrWhiteSpace(raw))
            return null;

        return _cache.GetOrAdd(raw, LoadBitmap);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }

    public void WarmUp(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            _cache.GetOrAdd(path, LoadBitmap);
        }
    }

    private static Bitmap? LoadBitmap(string raw)
    {
        try
        {
            if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == "avares")
                {
                    using var stream = AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }

                if (uri.IsFile && File.Exists(uri.LocalPath))
                    return new Bitmap(uri.LocalPath);
            }

            if (File.Exists(raw))
                return new Bitmap(raw);
        }
        catch
        {
            // ignored
        }

        return null;
    }
}
