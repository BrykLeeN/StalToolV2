using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StalTool.Models;

namespace StalTool.Services;

public class AuctionService
{
    public ObservableCollection<AuctionCategoryGroup> GetMockCategories()
    {
        return new ObservableCollection<AuctionCategoryGroup>
        {
            new()
            {
                CategoryName = "Оружие",
                Items = new ObservableCollection<AuctionCatalogItem>
                {
                    new() { ItemId = "ak74m", DisplayName = "АК-74М", Category = "Оружие", Rank = "epic" },
                    new() { ItemId = "svds", DisplayName = "СВДС", Category = "Оружие", Rank = "legendary" },
                    new() { ItemId = "ash12", DisplayName = "АШ-12", Category = "Оружие", Rank = "master" }
                }
            },
            new()
            {
                CategoryName = "Контейнеры",
                Items = new ObservableCollection<AuctionCatalogItem>
                {
                    new() { ItemId = "striker_case", DisplayName = "Ящик «Страйкер»", Category = "Контейнеры", Rank = "rare" },
                    new() { ItemId = "supply_box", DisplayName = "Снабженческий контейнер", Category = "Контейнеры", Rank = "common" }
                }
            },
            new()
            {
                CategoryName = "Броня",
                Items = new ObservableCollection<AuctionCatalogItem>
                {
                    new() { ItemId = "saturn", DisplayName = "Сатурн", Category = "Броня", Rank = "master" },
                    new() { ItemId = "skat9", DisplayName = "СКАТ-9", Category = "Броня", Rank = "epic" }
                }
            }
        };
    }

    public ObservableCollection<PricePoint> GetMockPriceBuffer(string itemId)
    {
        var now = DateTime.Now.Date;
        List<double> seededValues = itemId switch
        {
            "svds" => new() { 14800, 14650, 14520, 14480, 14320, 14150, 14000 },
            "ash12" => new() { 26500, 26200, 25800, 25500, 25100, 24800, 24500 },
            "striker_case" => new() { 920, 910, 905, 890, 880, 875, 870 },
            "saturn" => new() { 41200, 40900, 40500, 40100, 39850, 39500, 39200 },
            _ => new() { 5900, 5750, 5620, 5480, 5530, 5450, 5380 }
        };

        var points = new ObservableCollection<PricePoint>();
        const int daysInBuffer = 21;
        const int salesPerDay = 4;

        for (int dayOffset = daysInBuffer - 1; dayOffset >= 0; dayOffset--)
        {
            var date = now.AddDays(-dayOffset);
            var dayBase = seededValues[(daysInBuffer - 1 - dayOffset) % seededValues.Count];

            for (int saleIndex = 0; saleIndex < salesPerDay; saleIndex++)
            {
                var hour = 9 + (saleIndex * 3);
                var variance = (saleIndex - 1.5) * 35;
                points.Add(new PricePoint
                {
                    Time = date.AddHours(hour),
                    Value = dayBase + variance
                });
            }
        }

        return points;
    }

    public ObservableCollection<AuctionLot> GetMockActiveLots(string itemId)
    {
        var itemName = itemId switch
        {
            "svds" => "СВДС",
            "ash12" => "АШ-12",
            "striker_case" => "Ящик «Страйкер»",
            "saturn" => "Сатурн",
            _ => "АК-74М"
        };

        return new ObservableCollection<AuctionLot>
        {
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                Amount = 1,
                StartPrice = 5200,
                CurrentPrice = 5380,
                BuyoutPrice = 5450,
                StartTime = DateTime.Now.AddHours(-8),
                EndTime = DateTime.Now.AddHours(10),
                PriceStatus = "Низкая"
            },
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                Amount = 1,
                StartPrice = 5500,
                CurrentPrice = 5620,
                BuyoutPrice = 5700,
                StartTime = DateTime.Now.AddHours(-4),
                EndTime = DateTime.Now.AddHours(15),
                PriceStatus = "Норма"
            },
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                Amount = 1,
                StartPrice = 5900,
                CurrentPrice = 6100,
                BuyoutPrice = 6250,
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

        var changePercent = first == 0
            ? 0
            : ((last - first) / first) * 100.0;

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
