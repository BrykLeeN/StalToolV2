using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StalTool.Models;

public partial class RadarNotification : ObservableObject
{
    [ObservableProperty] private bool _isUnread = true;

    public string ItemName    { get; init; } = "";
    public string Description { get; init; } = "";
    public string TimeAgo     { get; init; } = "";
    public string PriceText   { get; init; } = "";
    public Color  PriceColor  { get; init; } = Color.Parse("#44FF88");
}
