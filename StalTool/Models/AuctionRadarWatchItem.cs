namespace StalTool.Models;

public class AuctionRadarWatchItem
{
    public string ItemName { get; set; } = string.Empty;
    public string CurrentPriceText { get; set; } = string.Empty;
    public string TargetPriceText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
