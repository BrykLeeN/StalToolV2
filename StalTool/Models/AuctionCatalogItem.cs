using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace StalTool.Models;

public partial class AuctionCatalogItem : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rank { get; set; } = "common";
    public string IconPath { get; set; } = string.Empty;
    public bool HasQualityVariants { get; set; }
    public ObservableCollection<AuctionCatalogItem> QualityVariants { get; set; } = new();

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded;
}
