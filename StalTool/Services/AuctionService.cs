using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StalTool.Models;

namespace StalTool.Services;

public class AuctionService
{
    private const string DefaultIcon = "avares://StalTool/Assets/appicons.png";

    public ObservableCollection<PricePoint> GetMockPriceBuffer(string itemId)
    {
        var now = DateTime.Now.Date;
        var seed = Math.Abs(itemId.GetHashCode(StringComparison.Ordinal));
        var basePrice = 1200 + (seed % 48000);
        var dayTrend = ((seed % 9) - 4) * 14.0;

        var points = new ObservableCollection<PricePoint>();
        const int daysInBuffer = 21;
        const int salesPerDay = 4;

        for (int dayOffset = daysInBuffer - 1; dayOffset >= 0; dayOffset--)
        {
            var date = now.AddDays(-dayOffset);
            var dayIndex = daysInBuffer - 1 - dayOffset;
            var seasonal = Math.Sin((dayIndex + (seed % 5)) / 3.5) * 90.0;
            var dayBase = basePrice + (dayTrend * dayIndex) + seasonal;

            for (int saleIndex = 0; saleIndex < salesPerDay; saleIndex++)
            {
                var hour = 9 + (saleIndex * 3);
                var variance = (saleIndex - 1.5) * 38;
                points.Add(new PricePoint
                {
                    Time = date.AddHours(hour),
                    Value = dayBase + variance
                });
            }
        }

        return points;
    }

    public ObservableCollection<AuctionLot> GetMockActiveLots(AuctionCatalogItem item)
    {
        var itemId = item.ItemId;
        var itemName = item.DisplayName;
        var seed = Math.Abs(itemId.GetHashCode(StringComparison.Ordinal));
        var basePrice = 1300 + (seed % 47000);
        var delta = Math.Max(35, basePrice * 0.018);

        return new ObservableCollection<AuctionLot>
        {
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                IconPath = string.IsNullOrWhiteSpace(item.IconPath) ? DefaultIcon : item.IconPath,
                Category = item.Category,
                Rank = item.Rank,
                Amount = 1,
                StartPrice = (long)Math.Round(basePrice - (delta * 0.9), 0),
                CurrentPrice = (long)Math.Round(basePrice - (delta * 0.4), 0),
                BuyoutPrice = (long)Math.Round(basePrice + (delta * 0.2), 0),
                StartTime = DateTime.Now.AddHours(-8),
                EndTime = DateTime.Now.AddHours(10),
                PriceStatus = "Низкая"
            },
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                IconPath = string.IsNullOrWhiteSpace(item.IconPath) ? DefaultIcon : item.IconPath,
                Category = item.Category,
                Rank = item.Rank,
                Amount = 1,
                StartPrice = (long)Math.Round(basePrice - (delta * 0.2), 0),
                CurrentPrice = (long)Math.Round(basePrice + (delta * 0.1), 0),
                BuyoutPrice = (long)Math.Round(basePrice + (delta * 0.45), 0),
                StartTime = DateTime.Now.AddHours(-4),
                EndTime = DateTime.Now.AddHours(15),
                PriceStatus = "Норма"
            },
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                IconPath = string.IsNullOrWhiteSpace(item.IconPath) ? DefaultIcon : item.IconPath,
                Category = item.Category,
                Rank = item.Rank,
                Amount = 1,
                StartPrice = (long)Math.Round(basePrice + (delta * 0.55), 0),
                CurrentPrice = (long)Math.Round(basePrice + (delta * 0.9), 0),
                BuyoutPrice = (long)Math.Round(basePrice + (delta * 1.25), 0),
                StartTime = DateTime.Now.AddHours(-1),
                EndTime = DateTime.Now.AddHours(22),
                PriceStatus = "Высокая"
            }
        };
    }

    public PriceStats BuildStats(IEnumerable<PricePoint> points)
    {
        var list = points.ToList();
        if (list.Count == 0)
        {
            return new PriceStats
            {
                TrendText = "Нет данных",
                RecommendationText = "Ожидание данных"
            };
        }

        var min = (long)list.Min(x => x.Value);
        var max = (long)list.Max(x => x.Value);
        var avg = (long)list.Average(x => x.Value);

        var first = list.First().Value;
        var last = list.Last().Value;
        var changePercent = first == 0 ? 0 : ((last - first) / first) * 100.0;

        var trendText = changePercent switch
        {
            > 3 => "Рост",
            < -3 => "Падение",
            _ => "Боковик"
        };

        var recommendation = changePercent switch
        {
            < -5 => "Рассмотреть покупку",
            > 5 => "Рассмотреть продажу",
            _ => "Наблюдать"
        };

        return new PriceStats
        {
            MinPrice = min,
            MaxPrice = max,
            AveragePrice = avg,
            ChangePercent = Math.Round(changePercent, 2),
            TrendText = trendText,
            RecommendationText = recommendation
        };
    }
}
