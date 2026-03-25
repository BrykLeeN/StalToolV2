using System;

namespace StalTool.Models;

public class AuctionHistoryPoint
{
    public long Price { get; set; }
    public long TotalPrice { get; set; } 
    public DateTime Time { get; set; }
    public int Amount { get; set; }
}
