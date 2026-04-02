using System.Collections.ObjectModel;
using StalTool.Models;

namespace StalTool.ViewModels.Auction.Sections;

public class AuctionRadarViewModel : Base.ViewModelBase
{
    public ObservableCollection<AuctionRadarWatchItem> ActiveItems { get; } = new();
    public ObservableCollection<AuctionRadarWatchItem> InactiveItems { get; } = new();
    public ObservableCollection<AuctionRadarSettingItem> Settings { get; } = new();

    public AuctionRadarViewModel()
    {
        ActiveItems.Add(new AuctionRadarWatchItem
        {
            ItemName = "АК-15 Тактический",
            CurrentPriceText = "12 420 ₽",
            TargetPriceText = "≤ 11 900 ₽",
            IsActive = true,
        });
        ActiveItems.Add(new AuctionRadarWatchItem
        {
            ItemName = "Контейнер «Крепость-6»",
            CurrentPriceText = "6 750 ₽",
            TargetPriceText = "≤ 6 400 ₽",
            IsActive = true,
        });

        InactiveItems.Add(new AuctionRadarWatchItem
        {
            ItemName = "СВД М1",
            CurrentPriceText = "18 300 ₽",
            TargetPriceText = "≤ 16 800 ₽",
            IsActive = false,
        });
        InactiveItems.Add(new AuctionRadarWatchItem
        {
            ItemName = "Артефакт «Кристалл»",
            CurrentPriceText = "4 900 ₽",
            TargetPriceText = "≤ 4 500 ₽",
            IsActive = false,
        });

        Settings.Add(new AuctionRadarSettingItem { Name = "Интервал обновления", Value = "Каждые 30 сек" });
        Settings.Add(new AuctionRadarSettingItem { Name = "Мин. разница до цели", Value = "3%" });
        Settings.Add(new AuctionRadarSettingItem { Name = "Канал уведомлений", Value = "Внутри приложения" });
        Settings.Add(new AuctionRadarSettingItem { Name = "Автозвук", Value = "Включен" });
    }
}
