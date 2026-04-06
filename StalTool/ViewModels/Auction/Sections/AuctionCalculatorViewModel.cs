using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Avalonia.Threading;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StalTool.Models;
using StalTool.Services;

namespace StalTool.ViewModels.Auction.Sections;

public partial class AuctionCalculatorViewModel : Base.ViewModelBase
{
    private const double AuctionSaleFee = 0.00;
    private const double DiscordSaleFee = 0.00;

    private readonly AuctionService _auctionService = new();
    private readonly CatalogService _catalogService = new();
    private readonly List<AuctionCategoryGroup> _allCategories = new();
    private readonly Dictionary<string, string> _itemSearchIndex = new();
    private readonly Dictionary<string, double> _marketPriceByItemId = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _searchDebounceTimer;
    private string _pendingSearchText = string.Empty;

    public AuctionCalculatorViewModel()
    {
        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _searchDebounceTimer.Tick += OnSearchDebounceTick;

        MarkupOptions = new ObservableCollection<double> { 5, 8, 10, 12, 15, 18, 20, 25, 30 };
        SaleChannels = new ObservableCollection<string> { "Продажа на аукционе", "Продажа в Discord" };
        Categories = new ObservableCollection<AuctionCategoryGroup>();
        ActiveLots = new ObservableCollection<AuctionLot>();

        LoadCategories();
        UpdateFeeText();
        RecalculateQuickValues();
    }

    public ObservableCollection<double> MarkupOptions { get; }
    public ObservableCollection<string> SaleChannels { get; }
    public ObservableCollection<AuctionCategoryGroup> Categories { get; }
    public ObservableCollection<AuctionLot> ActiveLots { get; }

    [ObservableProperty] private AuctionCatalogItem? _selectedItem;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isActiveLotsOverlayOpen;
    [ObservableProperty] private string _activeLotsHeaderText = "Активные лоты";
    [ObservableProperty] private double _activeLotsOverlayWidth = 760;
    [ObservableProperty] private double _activeLotsPriceColumnWidth = 140;

    [ObservableProperty] private string _quantityInput = "1";
    [ObservableProperty] private string _basePriceInput = string.Empty;
    [ObservableProperty] private string _purchasePriceInput = string.Empty;
    [ObservableProperty] private string _actualSellPriceInput = string.Empty;
    [ObservableProperty] private double _selectedMarkupPercent = 15;
    [ObservableProperty] private string _selectedSaleChannel = "Продажа на аукционе";

    [ObservableProperty] private string _offerBuyUnitPriceText = "—";
    [ObservableProperty] private string _offerBuyTotalText = "—";
    [ObservableProperty] private string _netSellTotalText = "—";
    [ObservableProperty] private string _grossSellTotalText = "—";
    [ObservableProperty] private string _profitPerUnitText = "—";
    [ObservableProperty] private string _profitTotalText = "Итог по партии появится после ввода цены и количества";
    [ObservableProperty] private IBrush _profitTotalBrush = Brushes.LightGray;
    [ObservableProperty] private string _breakEvenSellPriceText = "—";
    [ObservableProperty] private string _marketHintText = "Ориентиры появятся после выбора предмета.";
    [ObservableProperty] private string _recommendedSellUnitPriceText = "—";
    [ObservableProperty] private string _recommendedBuyUnitPriceText = "—";
    [ObservableProperty] private string _recommendedPartyText = "—";
    [ObservableProperty] private string _saleFeeText = "Комиссия продажи: 10%";

    [ObservableProperty] private string _quickBaseInput = string.Empty;
    [ObservableProperty] private string _quickPercentInput = string.Empty;
    [ObservableProperty] private string _quickResultText = "—";
    [ObservableProperty] private string _quickFromInput = string.Empty;
    [ObservableProperty] private string _quickToInput = string.Empty;
    [ObservableProperty] private string _quickChangeText = "—";
    [ObservableProperty] private string _expressionInput = string.Empty;
    [ObservableProperty] private string _expressionResultText = "—";

    partial void OnSelectedItemChanged(AuctionCatalogItem? value)
    {
        UpdateSelectedFlags();
        UpdateItemRecommendations();
        UpdateActiveLots();
        RecalculateTradeOutcome();
    }

    partial void OnSearchTextChanged(string value)
    {
        _pendingSearchText = value ?? string.Empty;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    partial void OnQuantityInputChanged(string value)
    {
        var normalized = NormalizeQuantityInput(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            QuantityInput = normalized;
            return;
        }

        UpdateItemRecommendations();
        RecalculateTradeOutcome();
    }

    partial void OnBasePriceInputChanged(string value)
    {
        var normalized = NormalizeMoneyInput(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            BasePriceInput = normalized;
            return;
        }

        RecalculateTradeOutcome();
    }

    partial void OnPurchasePriceInputChanged(string value)
    {
        var normalized = NormalizeMoneyInput(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            PurchasePriceInput = normalized;
            return;
        }

        RecalculateTradeOutcome();
    }

    partial void OnActualSellPriceInputChanged(string value)
    {
        var normalized = NormalizeMoneyInput(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            ActualSellPriceInput = normalized;
            return;
        }

        RecalculateTradeOutcome();
    }

    partial void OnSelectedMarkupPercentChanged(double value)
    {
        UpdateItemRecommendations();
        RecalculateTradeOutcome();
    }

    partial void OnSelectedSaleChannelChanged(string value)
    {
        UpdateFeeText();
        RecalculateTradeOutcome();
    }

    partial void OnQuickBaseInputChanged(string value) => RecalculateQuickValues();
    partial void OnQuickPercentInputChanged(string value) => RecalculateQuickValues();
    partial void OnQuickFromInputChanged(string value) => RecalculateQuickValues();
    partial void OnQuickToInputChanged(string value) => RecalculateQuickValues();
    partial void OnExpressionInputChanged(string value) => RecalculateQuickValues();

    [RelayCommand]
    private void ClearSearch()
    {
        if (string.IsNullOrEmpty(SearchText))
            return;

        SearchText = string.Empty;
    }

    [RelayCommand]
    private void SelectItem(AuctionCatalogItem? item)
    {
        if (item is null)
            return;

        SelectedItem = item;
        OpenActiveLotsOverlay();
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
        OpenActiveLotsOverlay();
    }

    [RelayCommand]
    private void CollapseCategoryByItem(AuctionCatalogItem? item)
    {
        if (item is null)
            return;

        var category = _allCategories.FirstOrDefault(c =>
            c.Items.Any(i =>
                i.ItemId == item.ItemId ||
                i.QualityVariants.Any(v => v.ItemId == item.ItemId)));

        if (category is null || !category.IsExpanded)
            return;

        category.IsExpanded = false;
        category.ShowCollapsedSelectedIndicator = category.HasSelectedItem;
    }

    [RelayCommand]
    private void CloseActiveLotsOverlay()
    {
        IsActiveLotsOverlayOpen = false;
    }

    [RelayCommand]
    private void ApplyLotToCalculator(AuctionLot? lot)
    {
        if (lot is null)
            return;

        var quantity = Math.Max(1, lot.Amount);
        QuantityInput = quantity.ToString(CultureInfo.InvariantCulture);
        BasePriceInput = lot.CurrentPrice.ToString(CultureInfo.InvariantCulture);
        PurchasePriceInput = lot.CurrentPrice.ToString(CultureInfo.InvariantCulture);
        IsActiveLotsOverlayOpen = false;
    }

    [RelayCommand]
    private void ApplyRecommendedSellPrice()
    {
        if (SelectedItem is null)
            return;

        var recommendedSell = GetApproxMarketSellUnitPrice(SelectedItem);
        var value = Math.Round(recommendedSell, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        BasePriceInput = value;
        if (string.IsNullOrWhiteSpace(ActualSellPriceInput))
            ActualSellPriceInput = value;
    }

    [RelayCommand]
    private void IncreaseQuantity()
    {
        var quantity = TryParseQuantity(QuantityInput, out var parsed) ? parsed : 1;
        quantity = Math.Min(quantity + 1, 99_999);
        QuantityInput = quantity.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void DecreaseQuantity()
    {
        var quantity = TryParseQuantity(QuantityInput, out var parsed) ? parsed : 1;
        quantity = Math.Max(quantity - 1, 1);
        QuantityInput = quantity.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void SetQuantityPreset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var preset) || preset <= 0)
            return;

        QuantityInput = preset.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void AddTradeToHistory()
    {
        // Кнопка добавлена заранее: сохранение истории подключим позже.
    }

    private void LoadCategories()
    {
        _allCategories.Clear();
        Categories.Clear();
        _itemSearchIndex.Clear();

        var cached = CloneCategories(_catalogService.GetCachedCategories());
        foreach (var category in cached.OrderBy(x => x.CategoryName))
        {
            category.FilteredItems = new ObservableCollection<AuctionCatalogItem>(category.Items);
            category.IsExpanded = false;
            category.HasSelectedItem = false;
            category.ShowCollapsedSelectedIndicator = false;

            foreach (var item in category.Items)
            {
                item.IsExpanded = false;
                item.IsSelected = false;
                _itemSearchIndex[item.ItemId] = BuildItemSearchIndex(item);

                foreach (var variant in item.QualityVariants)
                    variant.IsSelected = false;
            }

            _allCategories.Add(category);
            Categories.Add(category);
        }

        SelectedItem = GetFirstSelectableItem(Categories);
    }

    private void ApplyCategoryFilter()
    {
        var normalizedSearch = (_pendingSearchText ?? string.Empty).Trim().ToLowerInvariant();
        var hasSearch = normalizedSearch.Length > 0;
        var visibleCategories = new List<AuctionCategoryGroup>(_allCategories.Count);

        foreach (var category in _allCategories)
        {
            var categoryMatchesSearch = hasSearch &&
                                        category.CategoryName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase);
            var filteredItems = hasSearch
                ? category.Items.Where(x => categoryMatchesSearch || ItemMatchesSearch(x, normalizedSearch)).ToList()
                : category.Items.ToList();

            ReplaceFilteredItems(category.FilteredItems, filteredItems);
            if (category.FilteredItems.Count == 0)
                continue;

            if (hasSearch)
            {
                category.IsExpanded = true;
                foreach (var item in category.FilteredItems.Where(x => x.HasQualityVariants))
                {
                    item.IsExpanded = ItemOrVariantMatchesSearch(item, normalizedSearch);
                }
            }

            category.ShowCollapsedSelectedIndicator = category.HasSelectedItem && !category.IsExpanded;
            visibleCategories.Add(category);
        }

        ReplaceCategories(Categories, visibleCategories);
    }

    private static void ReplaceFilteredItems(ObservableCollection<AuctionCatalogItem> target, IReadOnlyList<AuctionCatalogItem> source)
    {
        if (target.Count == source.Count)
        {
            var sameOrder = true;
            for (int i = 0; i < source.Count; i++)
            {
                if (!ReferenceEquals(target[i], source[i]))
                {
                    sameOrder = false;
                    break;
                }
            }

            if (sameOrder)
                return;
        }

        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }

    private static void ReplaceCategories(ObservableCollection<AuctionCategoryGroup> target, IReadOnlyList<AuctionCategoryGroup> source)
    {
        if (target.Count == source.Count)
        {
            var sameOrder = true;
            for (int i = 0; i < source.Count; i++)
            {
                if (!ReferenceEquals(target[i], source[i]))
                {
                    sameOrder = false;
                    break;
                }
            }

            if (sameOrder)
                return;
        }

        target.Clear();
        foreach (var category in source)
            target.Add(category);
    }

    private static string BuildItemSearchIndex(AuctionCatalogItem item)
    {
        var parts = new List<string> { item.DisplayName };
        if (item.QualityVariants.Count > 0)
            parts.AddRange(item.QualityVariants.Select(x => x.DisplayName));

        return string.Join('\n', parts).ToLowerInvariant();
    }

    private bool ItemMatchesSearch(AuctionCatalogItem item, string normalizedSearch)
    {
        if (_itemSearchIndex.TryGetValue(item.ItemId, out var value))
            return value.Contains(normalizedSearch, StringComparison.Ordinal);

        var fallback = BuildItemSearchIndex(item);
        _itemSearchIndex[item.ItemId] = fallback;
        return fallback.Contains(normalizedSearch, StringComparison.Ordinal);
    }

    private static bool ItemOrVariantMatchesSearch(AuctionCatalogItem item, string normalizedSearch)
    {
        if (item.DisplayName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            return true;

        return item.QualityVariants.Any(v => v.DisplayName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
    }

    private void OpenActiveLotsOverlay()
    {
        if (SelectedItem is null)
            return;

        UpdateActiveLots();
        ActiveLotsHeaderText = $"Активные лоты: {SelectedItem.DisplayName}";
        UpdateActiveLotsLayoutMetrics();
        IsActiveLotsOverlayOpen = true;
    }

    private void UpdateActiveLots()
    {
        ActiveLots.Clear();
        if (SelectedItem is null)
            return;

        foreach (var lot in _auctionService.GetMockActiveLots(SelectedItem))
            ActiveLots.Add(lot);
    }

    private void UpdateActiveLotsLayoutMetrics()
    {
        var longestNameLength = SelectedItem?.DisplayName.Length ?? 0;
        if (ActiveLots.Count > 0)
            longestNameLength = Math.Max(longestNameLength, ActiveLots.Max(x => x.DisplayName?.Length ?? 0));

        // Минимум считаем от 100 млн, чтобы колонка цены не "ломалась" на крупных суммах.
        var maxPrice = 100_000_000L;
        foreach (var lot in ActiveLots)
            maxPrice = Math.Max(maxPrice, Math.Max(lot.CurrentPrice, Math.Max(lot.StartPrice, lot.BuyoutPrice)));

        var ruCulture = CultureInfo.GetCultureInfo("ru-RU");
        var maxPriceTextLength = maxPrice.ToString("N0", ruCulture).Length + 2; // + " ₽"
        var computedPriceColumnWidth = 116d + Math.Max(0, maxPriceTextLength - 7) * 5.8d;

        ActiveLotsPriceColumnWidth = Math.Clamp(computedPriceColumnWidth, 132d, 186d);

        var computedOverlayWidth = 380d + longestNameLength * 6.4d + ActiveLotsPriceColumnWidth;
        ActiveLotsOverlayWidth = Math.Clamp(computedOverlayWidth, 690d, 960d);
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

            category.HasSelectedItem = category.Items.Any(x => x.IsSelected || x.QualityVariants.Any(v => v.IsSelected));
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

    private void OnSearchDebounceTick(object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();
        ApplyCategoryFilter();
    }

    private void UpdateItemRecommendations()
    {
        if (SelectedItem is null)
        {
            RecommendedSellUnitPriceText = "—";
            RecommendedBuyUnitPriceText = "—";
            RecommendedPartyText = "—";
            MarketHintText = "Ориентиры появятся после выбора предмета.";
            return;
        }

        var qty = TryParseQuantity(QuantityInput, out var parsedQty) ? parsedQty : 1;
        var recommendedSellUnit = GetApproxMarketSellUnitPrice(SelectedItem);
        var markupRatio = SelectedMarkupPercent / 100.0;
        var recommendedBuyUnit = recommendedSellUnit / (1 + markupRatio);

        RecommendedSellUnitPriceText = FormatMoney(recommendedSellUnit);
        RecommendedBuyUnitPriceText = FormatMoney(recommendedBuyUnit);
        RecommendedPartyText =
            $"Ориентир по партии ({qty} шт): купить ~{FormatMoney(recommendedBuyUnit * qty)} / продать ~{FormatMoney(recommendedSellUnit * qty)}";
        MarketHintText = $"Ориентир для «{SelectedItem.DisplayName}». Это приблизительные значения по рынку.";
    }

    private void RecalculateTradeOutcome()
    {
        if (!TryParseQuantity(QuantityInput, out var quantity) || quantity <= 0)
        {
            OfferBuyUnitPriceText = "—";
            OfferBuyTotalText = "—";
            GrossSellTotalText = "—";
            NetSellTotalText = "—";
            ProfitPerUnitText = "—";
            ProfitTotalText = "—";
            ProfitTotalBrush = Brushes.LightGray;
            BreakEvenSellPriceText = "—";
            UpdateFeeText();
            return;
        }

        var hasBasePrice = TryParseMoney(BasePriceInput, out var basePartyPrice) && basePartyPrice > 0;
        if (hasBasePrice)
        {
            var baseMarkupRatio = SelectedMarkupPercent / 100.0;
            var offerBuyTotalOnly = basePartyPrice / (1 + baseMarkupRatio);
            var offerBuyUnitOnly = offerBuyTotalOnly / quantity;
            OfferBuyUnitPriceText = FormatMoney(offerBuyUnitOnly);
            OfferBuyTotalText = FormatMoney(offerBuyTotalOnly);
        }
        else
        {
            OfferBuyUnitPriceText = "—";
            OfferBuyTotalText = "—";
        }

        if (!TryParseMoney(ActualSellPriceInput, out var actualSellPartyPrice) || actualSellPartyPrice <= 0)
        {
            GrossSellTotalText = "—";
            NetSellTotalText = "—";
            ProfitPerUnitText = "—";
            ProfitTotalText = "—";
            ProfitTotalBrush = Brushes.LightGray;
            BreakEvenSellPriceText = "—";
            UpdateFeeText();
            return;
        }

        if (!TryParseMoney(PurchasePriceInput, out var purchasePartyPrice) || purchasePartyPrice <= 0)
        {
            GrossSellTotalText = FormatMoney(actualSellPartyPrice);
            var feeOnly = SelectedSaleChannel == "Продажа в Discord" ? DiscordSaleFee : AuctionSaleFee;
            NetSellTotalText = FormatMoney(actualSellPartyPrice * (1 - feeOnly));
            ProfitPerUnitText = "—";
            ProfitTotalText = "Введите цену покупки для расчёта прибыли";
            ProfitTotalBrush = Brushes.LightGray;
            BreakEvenSellPriceText = "—";
            UpdateFeeText();
            return;
        }

        var purchaseUnit = purchasePartyPrice / quantity;
        var fee = SelectedSaleChannel == "Продажа в Discord" ? DiscordSaleFee : AuctionSaleFee;
        var grossSellTotal = actualSellPartyPrice;
        var netSellTotal = grossSellTotal * (1 - fee);
        var netSellUnit = netSellTotal / quantity;
        var profitPerUnit = netSellUnit - purchaseUnit;
        var totalProfit = netSellTotal - purchasePartyPrice;
        var breakEvenSellUnit = purchaseUnit / (1 - fee);

        GrossSellTotalText = FormatMoney(grossSellTotal);
        NetSellTotalText = FormatMoney(netSellTotal);
        ProfitPerUnitText = $"{FormatSignedMoney(profitPerUnit)} ({(purchaseUnit == 0 ? 0 : profitPerUnit / purchaseUnit * 100):+0.##;-0.##;0}%)";
        ProfitTotalText = $"Итог по партии ({quantity} шт): {FormatSignedMoney(totalProfit)}";
        ProfitTotalBrush = totalProfit >= 0
            ? new SolidColorBrush(Color.Parse("#7FE9B0"))
            : new SolidColorBrush(Color.Parse("#FF8792"));
        BreakEvenSellPriceText = FormatMoney(breakEvenSellUnit);
        UpdateFeeText();
    }

    private void RecalculateQuickValues()
    {
        if (TryParseMoney(QuickBaseInput, out var quickBase) &&
            TryParsePercent(QuickPercentInput, out var quickPercent))
        {
            var quickValue = quickBase * quickPercent / 100.0;
            QuickResultText = $"{quickPercent:0.##}% от {FormatMoney(quickBase)} = {FormatMoney(quickValue)}";
        }
        else
        {
            QuickResultText = "—";
        }

        if (TryParseMoney(QuickFromInput, out var fromValue) &&
            TryParseMoney(QuickToInput, out var toValue) &&
            fromValue > 0)
        {
            var changePercent = (toValue - fromValue) / fromValue * 100.0;
            QuickChangeText = $"{FormatMoney(fromValue)} -> {FormatMoney(toValue)}: {changePercent:+0.##;-0.##;0}%";
        }
        else
        {
            QuickChangeText = "—";
        }

        if (TryEvaluateExpression(ExpressionInput, out var expressionValue))
        {
            ExpressionResultText = $"{expressionValue:0.####}";
        }
        else
        {
            ExpressionResultText = "—";
        }
    }

    private double GetApproxMarketSellUnitPrice(AuctionCatalogItem item)
    {
        if (_marketPriceByItemId.TryGetValue(item.ItemId, out var cached))
            return cached;

        var seed = HashCode.Combine(item.ItemId.ToLowerInvariant(), item.DisplayName.Length);
        var random = new Random(seed);
        var price = random.Next(5_500, 95_001);
        _marketPriceByItemId[item.ItemId] = price;
        return price;
    }

    private static bool TryParseMoney(string input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("₽", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseQuantity(string input, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input
            .Replace("шт", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("x", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
        return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParsePercent(string input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input
            .Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryEvaluateExpression(string input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Replace(" ", string.Empty, StringComparison.Ordinal).Replace(",", ".", StringComparison.Ordinal);
        foreach (var ch in normalized)
        {
            if (!(char.IsDigit(ch) || ch is '+' or '-' or '*' or '/' or '(' or ')' or '.'))
                return false;
        }

        try
        {
            var table = new DataTable { Locale = CultureInfo.InvariantCulture };
            var raw = table.Compute(normalized, string.Empty);
            if (raw is null)
                return false;

            return double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }
        catch
        {
            return false;
        }
    }

    private static string FormatMoney(double value)
    {
        return $"{Math.Round(value, MidpointRounding.AwayFromZero):N0} ₽";
    }

    private static string FormatSignedMoney(double value)
    {
        var prefix = value >= 0 ? "+" : "-";
        return $"{prefix}{FormatMoney(Math.Abs(value))}";
    }

    private void UpdateFeeText()
    {
        SaleFeeText = "Комиссия продажи: 0%";
    }

    private static string NormalizeMoneyInput(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length <= 12 ? digits : digits[..12];
    }

    private static string NormalizeQuantityInput(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length <= 5 ? digits : digits[..5];
    }

    private static ObservableCollection<AuctionCategoryGroup> CloneCategories(IEnumerable<AuctionCategoryGroup> categories)
    {
        var result = new ObservableCollection<AuctionCategoryGroup>();
        foreach (var category in categories)
        {
            result.Add(new AuctionCategoryGroup
            {
                CategoryName = category.CategoryName,
                Items = new ObservableCollection<AuctionCatalogItem>(category.Items.Select(CloneItem)),
                FilteredItems = new ObservableCollection<AuctionCatalogItem>()
            });
        }

        return result;
    }

    private static AuctionCatalogItem CloneItem(AuctionCatalogItem source)
    {
        var clone = new AuctionCatalogItem
        {
            ItemId = source.ItemId,
            DisplayName = source.DisplayName,
            Category = source.Category,
            Rank = source.Rank,
            IconPath = source.IconPath,
            HasQualityVariants = source.HasQualityVariants
        };

        clone.QualityVariants = new ObservableCollection<AuctionCatalogItem>(
            source.QualityVariants.Select(CloneItem));

        return clone;
    }
}
