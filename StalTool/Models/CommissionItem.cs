using System.Text.Json.Serialization;
using Avalonia.Media;

namespace SatlTool.Models;

public class CommissionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Icon { get; set; }
    public string JsonPath { get; set; }
    public decimal Weight { get; set; }
    public string RarityColorHex { get; set; }

    [JsonIgnore]
    public IBrush ColorBrush
    {
        get
        {
            if (string.IsNullOrEmpty(RarityColorHex)) return Brushes.White;
            try { return (SolidColorBrush)new BrushConverter().ConvertFrom(RarityColorHex); }
            catch { return Brushes.White; }
        }
    }
}
