using System.Collections.ObjectModel;

namespace StalTool.Models;

public class AuctionCategoryGroup
{
    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<AuctionCatalogItem> Items { get; set; } = new();
}