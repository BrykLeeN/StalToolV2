using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StalTool.Models;
using StalTool.Services;
using System.Threading.Tasks;

namespace StalTool.ViewModels.Auction.Sections;

public partial class AuctionPriceChartViewModel : Base.ViewModelBase
{
    private readonly AuctionService _auctionService;
    private readonly List<AuctionCategoryGroup> _allCategories = new();
    private readonly Dictionary<string, ObservableCollection<PricePoint>> _priceBufferByItem = new();
    private readonly Dictionary<string, bool> _expansionBeforeSearch = new();
    private bool _isSearchMode;

    public AuctionPriceChartViewModel()
    {
        _auctionService = new AuctionService();

        Categories = new ObservableCollection<AuctionCategoryGroup>();
        AvailableDays = new ObservableCollection<DateTime>();
        CalendarDays = new ObservableCollection<CalendarDayCell>();
        ActiveLots = new ObservableCollection<AuctionLot>();
        PriceHistory = new ObservableCollection<PricePoint>();
        ChartBars = new ObservableCollection<ChartBarItem>();

        LoadCategories();
        SelectedItem = Categories.SelectMany(x => x.Items).FirstOrDefault();
    }

    public ObservableCollection<AuctionCategoryGroup> Categories { get; }
    public ObservableCollection<DateTime> AvailableDays { get; }
    public ObservableCollection<CalendarDayCell> CalendarDays { get; }
    public ObservableCollection<AuctionLot> ActiveLots { get; }
    public ObservableCollection<PricePoint> PriceHistory { get; }
    public ObservableCollection<ChartBarItem> ChartBars { get; }

    [ObservableProperty]
    private AuctionCatalogItem? _selectedItem;

    [ObservableProperty]
    private DateTime? _selectedDay;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isCalendarOpen;

    [ObservableProperty]
    private PriceStats _stats = new();

    [ObservableProperty]
    private string _trendDisplayText = "0%";

    [ObservableProperty]
    private IBrush _trendBrush = new SolidColorBrush(Color.Parse("#A855F7"));

    public string SelectedItemName => SelectedItem?.DisplayName ?? "Предмет не выбран";
    public string SelectedDayText => SelectedDay?.ToString("dd.MM.yyyy") ?? "День не выбран";
    public string MinPriceText => $"{Stats.MinPrice:N0} ₽";
    public string AvgPriceText => $"{Stats.AveragePrice:N0} ₽";
    public string MaxPriceText => $"{Stats.MaxPrice:N0} ₽";
    public string CurrentPriceText => PriceHistory.Count > 0
        ? $"{PriceHistory.Last().Value:N0} ₽"
        : AvgPriceText;
    public string RecommendationText => Stats.RecommendationText;
    public string TrendText => Stats.TrendText;

    partial void OnSelectedItemChanged(AuctionCatalogItem? value)
    {
        UpdateSelectedFlags();
        OnPropertyChanged(nameof(SelectedItemName));

        if (value is null)
            return;

        EnsureBufferLoaded(value.ItemId);
        BuildAvailableDays(value.ItemId);
        ReloadData();
    }

    partial void OnSelectedDayChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(SelectedDayText));
        BuildCalendarDays();

        if (SelectedItem is not null)
            ReloadData();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyCategoryFilter();
    }

    partial void OnStatsChanged(PriceStats value)
    {
        OnPropertyChanged(nameof(CurrentPriceText));
        OnPropertyChanged(nameof(MinPriceText));
        OnPropertyChanged(nameof(AvgPriceText));
        OnPropertyChanged(nameof(MaxPriceText));
        OnPropertyChanged(nameof(RecommendationText));
        OnPropertyChanged(nameof(TrendText));
    }

    [RelayCommand]
    private void SelectItem(AuctionCatalogItem? item)
    {
        if (item is not null)
            SelectedItem = item;
    }

    [RelayCommand]
    private void SelectDay(DateTime? day)
    {
        if (day is null)
            return;

        var selected = day.Value.Date;
        if (AvailableDays.Contains(selected))
        {
            SelectedDay = selected;
            IsCalendarOpen = false;
        }
    }

    [RelayCommand]
    private void ToggleCalendar()
    {
        IsCalendarOpen = !IsCalendarOpen;
    }

    [RelayCommand]
    private void CloseCalendar()
    {
        IsCalendarOpen = false;
    }

    private void LoadCategories()
    {
        LoadCategoriesInternal(_auctionService.GetCategoriesFromCacheOrMock());
        _ = RefreshCategoriesFromGitHubAsync();
    }

    private void LoadCategoriesInternal(IEnumerable<AuctionCategoryGroup> groups)
    {
        _allCategories.Clear();
        Categories.Clear();

        foreach (var category in groups)
        {
            category.FilteredItems = new ObservableCollection<AuctionCatalogItem>(category.Items);
            category.IsExpanded = false;
            category.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(AuctionCategoryGroup.IsExpanded))
                    category.ShowCollapsedSelectedIndicator = category.HasSelectedItem && !category.IsExpanded;
            };

            _allCategories.Add(category);
            Categories.Add(category);
        }
    }

    private async Task RefreshCategoriesFromGitHubAsync()
    {
        var remote = await _auctionService.RefreshCategoriesFromGitHubAsync();
        if (remote.Count == 0)
            return;

        var selectedId = SelectedItem?.ItemId;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LoadCategoriesInternal(remote);
            ApplyCategoryFilter();

            var candidate = !string.IsNullOrWhiteSpace(selectedId)
                ? Categories.SelectMany(x => x.Items).FirstOrDefault(x => x.ItemId == selectedId)
                : null;
            SelectedItem = candidate ?? Categories.SelectMany(x => x.Items).FirstOrDefault();
        });
    }

    private void ApplyCategoryFilter()
    {
        var hasSearch = !string.IsNullOrWhiteSpace(SearchText);
        var search = SearchText.Trim();
        var startedSearch = hasSearch && !_isSearchMode;
        var finishedSearch = !hasSearch && _isSearchMode;

        if (startedSearch)
        {
            _expansionBeforeSearch.Clear();
            foreach (var category in _allCategories)
                _expansionBeforeSearch[category.CategoryName] = category.IsExpanded;
        }

        Categories.Clear();

        foreach (var category in _allCategories)
        {
            var filteredItems = hasSearch
                ? category.Items.Where(x => x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList()
                : category.Items.ToList();

            category.FilteredItems.Clear();
            foreach (var item in filteredItems)
                category.FilteredItems.Add(item);

            if (category.FilteredItems.Count == 0)
                continue;

            if (hasSearch)
                category.IsExpanded = true;
            else if (finishedSearch && _expansionBeforeSearch.TryGetValue(category.CategoryName, out var wasExpanded))
                category.IsExpanded = wasExpanded;

            category.ShowCollapsedSelectedIndicator = category.HasSelectedItem && !category.IsExpanded;

            Categories.Add(category);
        }

        _isSearchMode = hasSearch;
    }

    private void EnsureBufferLoaded(string itemId)
    {
        if (_priceBufferByItem.ContainsKey(itemId))
            return;

        _priceBufferByItem[itemId] = _auctionService.GetMockPriceBuffer(itemId);
    }

    private void BuildAvailableDays(string itemId)
    {
        AvailableDays.Clear();

        foreach (var day in _priceBufferByItem[itemId]
                     .Select(x => x.Time.Date)
                     .Distinct()
                     .OrderByDescending(x => x))
        {
            AvailableDays.Add(day);
        }

        if (SelectedDay is null || !AvailableDays.Contains(SelectedDay.Value.Date))
            SelectedDay = AvailableDays.Count > 0 ? AvailableDays[0] : null;

        BuildCalendarDays();
    }

    private void BuildCalendarDays()
    {
        CalendarDays.Clear();

        if (AvailableDays.Count == 0 || SelectedDay is null)
            return;

        var selectedDate = SelectedDay.Value.Date;
        var monthStart = new DateTime(selectedDate.Year, selectedDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var availableSet = AvailableDays.Select(x => x.Date).ToHashSet();

        var firstDayOffset = ((int)monthStart.DayOfWeek + 6) % 7;
        for (int i = 0; i < firstDayOffset; i++)
        {
            CalendarDays.Add(new CalendarDayCell
            {
                IsPlaceholder = true,
                IsUnavailable = true
            });
        }

        for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
        {
            var isAvailable = availableSet.Contains(day.Date);
            CalendarDays.Add(new CalendarDayCell
            {
                Date = day.Date,
                DayNumber = day.Day.ToString(),
                IsAvailable = isAvailable,
                IsUnavailable = !isAvailable,
                IsSelected = day.Date == selectedDate
            });
        }
    }

    private void UpdateSelectedFlags()
    {
        foreach (var category in _allCategories)
        {
            foreach (var item in category.Items)
                item.IsSelected = SelectedItem is not null && item.ItemId == SelectedItem.ItemId;

            category.HasSelectedItem = category.Items.Any(x => x.IsSelected);
            category.ShowCollapsedSelectedIndicator = category.HasSelectedItem && !category.IsExpanded;
        }
    }

    private void ReloadData()
    {
        if (SelectedItem is null || SelectedDay is null)
            return;

        var selectedDate = SelectedDay.Value.Date;
        var startDate = selectedDate.AddDays(-2);

        PriceHistory.Clear();
        foreach (var point in _priceBufferByItem[SelectedItem.ItemId]
                     .Where(x => x.Time.Date >= startDate && x.Time.Date <= selectedDate)
                     .OrderBy(x => x.Time))
        {
            PriceHistory.Add(point);
        }
        OnPropertyChanged(nameof(CurrentPriceText));

        ActiveLots.Clear();
        foreach (var lot in _auctionService.GetMockActiveLots(SelectedItem))
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
            var label = point.Time.Date == SelectedDay?.Date
                ? point.Time.ToString("HH:mm")
                : point.Time.ToString("dd.MM");

            ChartBars.Add(new ChartBarItem
            {
                Label = label,
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

    public partial class CalendarDayCell : ObservableObject
    {
        public DateTime? Date { get; set; }
        public string DayNumber { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isAvailable;

        [ObservableProperty]
        private bool _isUnavailable;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isPlaceholder;
    }
}
