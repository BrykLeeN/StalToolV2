using System;

namespace StalTool.Models;

public class PricePoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
    public int Amount { get; set; } = 1;
    public int EnhancementLevel { get; set; }
    public string Label => Time.ToString("dd.MM");
}
