using System;

namespace StalTool.Models;

public class PricePoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
    public string Label => Time.ToString("dd.MM");
}