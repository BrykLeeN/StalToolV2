using System;
using System.Collections.Generic;

public class AuctionLot
{
    public string itemId { get; set; }
    public int amount { get; set; }
    public long startPrice { get; set; }
    public long currentPrice { get; set; }
    public long buyoutPrice { get; set; }
    public DateTime startTime { get; set; }
    public DateTime endTime { get; set; }
}

public class AuctionResponse
{
    public long total { get; set; }
    public List<AuctionLot> lots { get; set; }
}

// --- А ЭТО НОВОЕ: ДЛЯ ГРАФИКА (ИСТОРИЯ ПРОДАЖ) ---
public class HistoryPrice
{
    public int amount { get; set; }
    public long price { get; set; }
    public DateTime time { get; set; }
}

public class AuctionHistoryResponse
{
    public long total { get; set; }
    public List<HistoryPrice> prices { get; set; }
}