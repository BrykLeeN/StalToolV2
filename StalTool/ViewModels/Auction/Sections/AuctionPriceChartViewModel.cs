using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StalTool.Models;
using StalTool.Services;

namespace StalTool.ViewModels.Auction.Sections;

public partial class AuctionPriceChartViewModel : Base.ViewModelBase
{
    private readonly AuctionService _auctionService;

    public AuctionPriceChartViewModel()
    {
        _auctionService = new AuctionService();

        Periods = new ObservableCollection<string> { "1D", "3D", "7D", "30D" };
        Categories = new ObservableCollection<AuctionCategoryGroup>();
        ActiveLots = new ObservableCollection<AuctionLot>();
        PriceHistory = new ObservableCollection<PricePoint>();
        ChartBars = new ObservableCollection<ChartBarItem>();

        LoadCategories();
        SelectedPeriod = "3D";
        SelectedItem = Categories.SelectMany(x => x.Items).FirstOrDefault();
    }

    public ObservableCollection<string> Periods { get; }

    public ObservableCollection<AuctionCategoryGroup> Categories { get; }

    public ObservableCollection<AuctionLot> ActiveLots { get; }

    public ObservableCollection<PricePoint> PriceHistory { get; }

    public ObservableCollection<ChartBarItem> ChartBars { get; }

    [ObservableProperty]
    private AuctionCatalogItem? _selectedItem;

    [ObservableProperty]
    private string _selectedPeriod = "3D";

    [ObservableProperty]
    private PriceStats _stats = new();

    [ObservableProperty]
    private string _trendDisplayText = "0%";

    [ObservableProperty]
    private IBrush _trendBrush = new SolidColorBrush(Color.Parse("#A855F7"));

    public string SelectedItemName => SelectedItem?.DisplayName ?? "Предмет не выбран";
    public string MinPriceText => $"{Stats.MinPrice:N0} ₽";
    public string AvgPriceText => $"{Stats.AveragePrice:N0} ₽";
    public string MaxPriceText => $"{Stats.MaxPrice:N0} ₽";
    public string RecommendationText => Stats.RecommendationText;
    public string TrendText => Stats.TrendText;

    partial void OnSelectedItemChanged(AuctionCatalogItem? value)
    {
        OnPropertyChanged(nameof(SelectedItemName));

        if (value is not null)
            ReloadData();
    }

    partial void OnSelectedPeriodChanged(string value)
    {
        if (SelectedItem is not null)
            ReloadData();
    }

    partial void OnStatsChanged(PriceStats value)
    {
        OnPropertyChanged(nameof(MinPriceText));
        OnPropertyChanged(nameof(AvgPriceText));
        OnPropertyChanged(nameof(MaxPriceText));
        OnPropertyChanged(nameof(RecommendationText));
        OnPropertyChanged(nameof(TrendText));
    }

    [RelayCommand]
    private void SetPeriod(string? period)
    {
        if (!string.IsNullOrWhiteSpace(period))
            SelectedPeriod = period;
    }

    [RelayCommand]
    private void SelectItem(AuctionCatalogItem? item)
    {
        if (item is not null)
            SelectedItem = item;
    }

    private void LoadCategories()
    {
        Categories.Clear();

        foreach (var category in _auctionService.GetMockCategories())
            Categories.Add(category);
    }

    private void ReloadData()
    {
        if (SelectedItem is null)
            return;

        PriceHistory.Clear();
        foreach (var point in _auctionService.GetMockPriceHistory(SelectedItem.ItemId, SelectedPeriod))
            PriceHistory.Add(point);

        ActiveLots.Clear();
        foreach (var lot in _auctionService.GetMockActiveLots(SelectedItem.ItemId))
            ActiveLots.Add(lot);

        Stats = _auctionService.BuildStats(PriceHistory);
        TrendDisplayText = Stats.ChangePercent >= 0
            ? $"+{Stats.ChangePercent:0.##}%"
            : $"{Stats.ChangePercent:0.##}%";

        TrendBrush = Stats.ChangePercent switch
        {
            > 0 => new SolidColorBrush(Color.Parse("#44FF88")),
            < 0 => new SolidColorBrush(Color.Parse("#FF5566")),
            _ => new SolidColorBrush(Color.Parse("#A855F7"))
        };

        BuildChartBars();
    }

    private void BuildChartBars()
    {
        ChartBars.Clear();

        if (PriceHistory.Count == 0)
            return;

        var max = PriceHistory.Max(x => x.Value);
        if (max <= 0)
            max = 1;

        foreach (var point in PriceHistory)
        {
            var normalized = point.Value / max;
            var height = 22 + (normalized * 170);

            ChartBars.Add(new ChartBarItem
            {
                Label = point.Label,
                ValueText = $"{point.Value:N0}",
                Height = height,
                Fill = CreateBarBrush(normalized)
            });
        }
    }

    private static IBrush CreateBarBrush(double normalized)
    {
        if (normalized >= 0.85)
            return new SolidColorBrush(Color.Parse("#D946EF"));

        if (normalized >= 0.60)
            return new SolidColorBrush(Color.Parse("#A855F7"));

        return new SolidColorBrush(Color.Parse("#7C3AED"));
    }

    public partial class ChartBarItem : ObservableObject
    {
        public string Label { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
        public double Height { get; set; }
        public IBrush Fill { get; set; } = Brushes.Transparent;
    }
}