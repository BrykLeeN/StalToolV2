using CommunityToolkit.Mvvm.ComponentModel;

namespace StalTool.Models;

public partial class AuctionCatalogItem : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rank { get; set; } = "common";
    public string IconPath { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
