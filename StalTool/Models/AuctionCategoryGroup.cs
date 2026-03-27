using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StalTool.Models;

public partial class AuctionCategoryGroup : ObservableObject
{
    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<AuctionCatalogItem> Items { get; set; } = new();
    public ObservableCollection<AuctionCatalogItem> FilteredItems { get; set; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _hasSelectedItem;

    [ObservableProperty]
    private bool _showCollapsedSelectedIndicator;
}
