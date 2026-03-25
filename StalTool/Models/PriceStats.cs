namespace StalTool.Models;

public class PriceStats
{
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public long AveragePrice { get; set; }
    
    public double ChangePercent { get; set; }
    
    public string TrendText { get; set; } = string.Empty;
    public string RecommendationText { get; set; } = string.Empty;
}