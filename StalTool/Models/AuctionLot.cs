using System;

namespace StalTool.Models;

public class AuctionLot
{
    public string ItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;

    public int Amount { get; set; }
    public long StartPrice { get; set; }
    public long CurrentPrice { get; set; }
    public long BuyoutPrice { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string PriceStatus { get; set; } = "Норма";
    public bool ShowAmountBadge { get; set; } = true;
}
