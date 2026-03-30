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
    private readonly List<PricePoint> _currentSales = new();
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
        EnhancementFilters = new ObservableCollection<EnhancementFilterOption>();

        BuildEnhancementFilters();
        LoadCategories();
        SelectedItem = GetFirstSelectableItem(Categories);
    }

    public ObservableCollection<AuctionCategoryGroup> Categories { get; }
    public ObservableCollection<DateTime> AvailableDays { get; }
    public ObservableCollection<CalendarDayCell> CalendarDays { get; }
    public ObservableCollection<AuctionLot> ActiveLots { get; }
    public ObservableCollection<PricePoint> PriceHistory { get; }
    public ObservableCollection<ChartBarItem> ChartBars { get; }
    public ObservableCollection<EnhancementFilterOption> EnhancementFilters { get; }

    [ObservableProperty]
    private AuctionCatalogItem? _selectedItem;

    [ObservableProperty]
    private DateTime? _selectedDay;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isCalendarOpen;

    [ObservableProperty]
    private bool _isEnhancementFilterOpen;

    [ObservableProperty]
    private PriceStats _stats = new();

    [ObservableProperty]
    private string _trendDisplayText = "0%";

    [ObservableProperty]
    private IBrush _trendBrush = new SolidColorBrush(Color.Parse("#A855F7"));

    [ObservableProperty]
    private string _selectedSaleText = "Продажа не выбрана";

    [ObservableProperty]
    private string _selectedAverageText = "Выберите столбцы для усреднения";

    [ObservableProperty]
    private EnhancementFilterOption? _selectedEnhancementFilter;

    public string SelectedItemName => SelectedItem?.DisplayName ?? "Предмет не выбран";
    public string SelectedDayText => SelectedDay?.ToString("dd.MM.yyyy") ?? "День не выбран";
    public string SelectedEnhancementFilterText => SelectedEnhancementFilter?.Title ?? "Все заточки";
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

    partial void OnSelectedEnhancementFilterChanged(EnhancementFilterOption? value)
    {
        foreach (var filter in EnhancementFilters)
            filter.IsSelected = filter == value;

        OnPropertyChanged(nameof(SelectedEnhancementFilterText));
        IsEnhancementFilterOpen = false;
        ApplyEnhancementFilter();
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
    private void ToggleOrSelectItem(AuctionCatalogItem? item)
    {
        if (item is null)
            return;

        if (item.HasQualityVariants)
        {
            item.IsExpanded = !item.IsExpanded;
            return;
        }

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

    [RelayCommand]
    private void ToggleEnhancementFilter()
    {
        IsEnhancementFilterOpen = !IsEnhancementFilterOpen;
    }

    [RelayCommand]
    private void SelectEnhancementFilter(EnhancementFilterOption? filter)
    {
        if (filter is not null)
            SelectedEnhancementFilter = filter;
    }

    [RelayCommand]
    private void CloseEnhancementFilter()
    {
        IsEnhancementFilterOpen = false;
    }

    [RelayCommand]
    private void ToggleChartBarSelection(ChartBarItem? bar)
    {
        if (bar is null)
            return;

        bar.IsSelected = !bar.IsSelected;
        SelectedSaleText = $"Продажа: {bar.Time:dd.MM HH:mm} - {bar.Value:N0} ₽";
        UpdateBarBrushes();
        UpdateSelectedAverageText();
    }

    [RelayCommand]
    private void ClearSaleSelection()
    {
        if (!ChartBars.Any(x => x.IsSelected))
            return;

        foreach (var bar in ChartBars)
            bar.IsSelected = false;

        SelectedSaleText = "Продажа не выбрана";
        UpdateBarBrushes();
        SelectedAverageText = "Выберите столбцы для усреднения";
    }

    private void LoadCategories()
    {
        LoadCategoriesInternal(_auctionService.GetCachedCategories());
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
            foreach (var item in category.Items)
            {
                item.IsExpanded = false;
                item.IsSelected = false;
                foreach (var variant in item.QualityVariants)
                    variant.IsSelected = false;
            }
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
                ? Categories
                    .SelectMany(x => x.Items)
                    .SelectMany(GetSelectableItems)
                    .FirstOrDefault(x => x.ItemId == selectedId)
                : null;
            SelectedItem = candidate ?? GetFirstSelectableItem(Categories);
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
                ? category.Items.Where(x =>
                        x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        category.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        x.QualityVariants.Any(v => v.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList()
                : category.Items.ToList();

            foreach (var item in filteredItems.Where(x => x.HasQualityVariants))
            {
                item.IsExpanded = hasSearch && (
                    item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.QualityVariants.Any(v => v.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

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
            {
                if (item.HasQualityVariants)
                {
                    var selectedVariant = item.QualityVariants
                        .FirstOrDefault(v => SelectedItem is not null && v.ItemId == SelectedItem.ItemId);
                    item.IsSelected = selectedVariant is not null;
                    if (selectedVariant is not null)
                        item.IsExpanded = true;
                    foreach (var variant in item.QualityVariants)
                        variant.IsSelected = selectedVariant is not null && variant.ItemId == selectedVariant.ItemId;
                }
                else
                {
                    item.IsSelected = SelectedItem is not null && item.ItemId == SelectedItem.ItemId;
                }
            }

            category.HasSelectedItem = category.Items.Any(x =>
                x.IsSelected || x.QualityVariants.Any(v => v.IsSelected));
            category.ShowCollapsedSelectedIndicator = category.HasSelectedItem && !category.IsExpanded;
        }
    }

    private static IEnumerable<AuctionCatalogItem> GetSelectableItems(AuctionCatalogItem item)
    {
        return item.HasQualityVariants ? item.QualityVariants : new[] { item };
    }

    private static AuctionCatalogItem? GetFirstSelectableItem(IEnumerable<AuctionCategoryGroup> categories)
    {
        return categories.SelectMany(x => x.Items).SelectMany(GetSelectableItems).FirstOrDefault();
    }

    private void BuildEnhancementFilters()
    {
        EnhancementFilters.Clear();
        EnhancementFilters.Add(new EnhancementFilterOption
        {
            Title = "Все заточки",
            Level = null,
            IsSelected = true
        });

        for (int level = 0; level <= 15; level++)
        {
            EnhancementFilters.Add(new EnhancementFilterOption
            {
                Title = $"+{level}",
                Level = level
            });
        }

        SelectedEnhancementFilter = EnhancementFilters[0];
    }

    private void ReloadData()
    {
        if (SelectedItem is null || SelectedDay is null)
            return;

        var selectedDate = SelectedDay.Value.Date;
        _currentSales.Clear();
        _currentSales.AddRange(GenerateRandomSales(SelectedItem.ItemId, selectedDate));

        ActiveLots.Clear();
        foreach (var lot in _auctionService.GetMockActiveLots(SelectedItem))
            ActiveLots.Add(lot);

        ApplyEnhancementFilter();
    }

    private List<PricePoint> GenerateRandomSales(string itemId, DateTime selectedDate)
    {
        var seed = HashCode.Combine(itemId.ToLowerInvariant(), selectedDate.Date);
        var random = new Random(seed);
        var count = random.Next(16, 34);
        var points = new List<PricePoint>(count);

        for (int i = 0; i < count; i++)
        {
            var minute = (int)Math.Round((double)i / Math.Max(1, count - 1) * (24 * 60 - 1));
            minute = Math.Clamp(minute + random.Next(-20, 21), 0, 24 * 60 - 1);
            var value = random.NextInt64(100_000, 30_000_001);

            points.Add(new PricePoint
            {
                Time = selectedDate.Date.AddMinutes(minute),
                Value = value,
                EnhancementLevel = random.Next(0, 16)
            });
        }

        return points.OrderBy(x => x.Time).ToList();
    }

    private void ApplyEnhancementFilter()
    {
        var enhancementLevel = SelectedEnhancementFilter?.Level;
        var filteredSales = enhancementLevel.HasValue
            ? _currentSales.Where(x => x.EnhancementLevel == enhancementLevel.Value).ToList()
            : _currentSales.ToList();

        PriceHistory.Clear();
        foreach (var point in filteredSales)
            PriceHistory.Add(point);
        OnPropertyChanged(nameof(CurrentPriceText));

        Stats = _auctionService.BuildStats(filteredSales);
        TrendDisplayText = Stats.ChangePercent >= 0
            ? $"+{Stats.ChangePercent:0.##}%"
            : $"{Stats.ChangePercent:0.##}%";

        TrendBrush = Stats.ChangePercent switch
        {
            > 0 => new SolidColorBrush(Color.Parse("#44FF88")),
            < 0 => new SolidColorBrush(Color.Parse("#FF5566")),
            _ => new SolidColorBrush(Color.Parse("#A855F7"))
        };

        BuildChartBars(filteredSales);
        if (filteredSales.Count == 0 && enhancementLevel.HasValue)
            SelectedSaleText = $"Нет продаж для заточки +{enhancementLevel.Value}";
    }

    private void BuildChartBars(IReadOnlyList<PricePoint> sales)
    {
        ChartBars.Clear();
        SelectedSaleText = "Продажа не выбрана";
        SelectedAverageText = "Выберите столбцы для усреднения";

        if (sales.Count == 0)
            return;

        var max = sales.Max(x => x.Value);
        var min = sales.Min(x => x.Value);
        if (max <= min)
            max = min + 1;

        foreach (var sale in sales)
        {
            var normalized = (sale.Value - min) / (double)(max - min);
            var emphasized = Math.Pow(normalized, 0.7);
            var height = 40 + (emphasized * 212);
            ChartBars.Add(new ChartBarItem
            {
                Time = sale.Time,
                Value = sale.Value,
                Label = sale.Time.ToString("HH:mm"),
                ValueText = $"{sale.Value:N0} ₽",
                EnhancementText = $"+{sale.EnhancementLevel}",
                Height = height,
                IsSelected = false,
                Fill = CreateBarBrush(normalized, isSelected: false)
            });
        }
    }

    private void UpdateSelectedAverageText()
    {
        var selected = ChartBars.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SelectedAverageText = "Выберите столбцы для усреднения";
            return;
        }

        var average = selected.Average(x => x.Value);
        SelectedAverageText = $"Выбрано: {selected.Count} | Средняя: {average:N0} ₽";
    }

    private void UpdateBarBrushes()
    {
        if (ChartBars.Count == 0)
            return;

        var max = ChartBars.Max(x => x.Value);
        var min = ChartBars.Min(x => x.Value);
        if (max <= min)
            max = min + 1;

        foreach (var bar in ChartBars)
        {
            var normalized = (bar.Value - min) / (max - min);
            bar.Fill = CreateBarBrush(normalized, bar.IsSelected);
        }
    }

    private static IBrush CreateBarBrush(double normalized, bool isSelected)
    {
        if (isSelected)
            return new SolidColorBrush(Color.Parse("#FFD45A"));

        if (normalized >= 0.85)
            return new SolidColorBrush(Color.Parse("#D946EF"));
        if (normalized >= 0.60)
            return new SolidColorBrush(Color.Parse("#A855F7"));
        return new SolidColorBrush(Color.Parse("#7C3AED"));
    }

    public partial class ChartBarItem : ObservableObject
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
        public string EnhancementText { get; set; } = string.Empty;
        public double Height { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private IBrush _fill = Brushes.Transparent;
    }

    public partial class EnhancementFilterOption : ObservableObject
    {
        public string Title { get; set; } = string.Empty;
        public int? Level { get; set; }

        [ObservableProperty]
        private bool _isSelected;
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
