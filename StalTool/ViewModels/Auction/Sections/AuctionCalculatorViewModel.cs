using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StalTool.ViewModels.Auction.Sections;

public partial class AuctionCalculatorViewModel : Base.ViewModelBase
{
    private const double AuctionSaleFee = 0.10;
    private const double DiscordSaleFee = 0.03;
    private const double ThreeDayAveragePrice = 12600;
    private const double ActiveLotsAveragePrice = 12150;

    public ObservableCollection<double> MarkupOptions { get; } = new()
    {
        5, 8, 10, 12, 15, 18, 20, 25
    };

    public ObservableCollection<string> SaleChannels { get; } = new()
    {
        "Продажа на аукционе",
        "Продажа в Discord",
    };

    [ObservableProperty] private string _targetSellPriceInput = string.Empty;
    [ObservableProperty] private double _selectedMarkupPercent = 10;
    [ObservableProperty] private string _maxBuyPriceText = "—";
    [ObservableProperty] private string _recommendedBuyPriceText = "—";
    [ObservableProperty] private string _marketReferenceText =
        "Данные пока заглушка: активные лоты + история за 3 дня";

    [ObservableProperty] private string _soldPriceInput = string.Empty;
    [ObservableProperty] private string _purchasePriceInput = string.Empty;
    [ObservableProperty] private string _selectedSaleChannel = "Продажа на аукционе";
    [ObservableProperty] private string _saleOutcomeText = "Введите сумму продажи и закупки";
    [ObservableProperty] private IBrush _saleOutcomeBrush = Brushes.LightGray;

    [ObservableProperty] private string _quickBaseInput = string.Empty;
    [ObservableProperty] private string _quickPercentInput = string.Empty;
    [ObservableProperty] private string _quickResultText = "—";
    [ObservableProperty] private string _quickFromInput = string.Empty;
    [ObservableProperty] private string _quickToInput = string.Empty;
    [ObservableProperty] private string _quickChangeText = "—";

    [ObservableProperty] private string _analyticsSummary =
        "Аналитика в разработке: позже здесь будет прогноз, когда покупать/продавать по тренду и активным лотам.";

    public AuctionCalculatorViewModel()
    {
        RecalculateTradePlan();
        RecalculateSaleOutcome();
        RecalculateQuickValues();
    }

    partial void OnTargetSellPriceInputChanged(string value) => RecalculateTradePlan();
    partial void OnSelectedMarkupPercentChanged(double value) => RecalculateTradePlan();
    partial void OnSoldPriceInputChanged(string value) => RecalculateSaleOutcome();
    partial void OnPurchasePriceInputChanged(string value) => RecalculateSaleOutcome();
    partial void OnSelectedSaleChannelChanged(string value) => RecalculateSaleOutcome();
    partial void OnQuickBaseInputChanged(string value) => RecalculateQuickValues();
    partial void OnQuickPercentInputChanged(string value) => RecalculateQuickValues();
    partial void OnQuickFromInputChanged(string value) => RecalculateQuickValues();
    partial void OnQuickToInputChanged(string value) => RecalculateQuickValues();

    private void RecalculateTradePlan()
    {
        if (!TryParseMoney(TargetSellPriceInput, out var targetSellPrice) || targetSellPrice <= 0)
        {
            MaxBuyPriceText = "—";
            RecommendedBuyPriceText = "—";
            return;
        }

        var markupRatio = SelectedMarkupPercent / 100.0;
        var maxBuyPrice = targetSellPrice / (1 + markupRatio);
        var marketBaseline = (ThreeDayAveragePrice * 0.55) + (ActiveLotsAveragePrice * 0.45);
        var recommendedBuy = Math.Min(maxBuyPrice, marketBaseline * (1 - markupRatio * 0.55));

        MaxBuyPriceText = FormatMoney(maxBuyPrice);
        RecommendedBuyPriceText = FormatMoney(recommendedBuy);
        MarketReferenceText = $"Основа расчета: 3 дня {FormatMoney(ThreeDayAveragePrice)} и активные лоты {FormatMoney(ActiveLotsAveragePrice)}";

        if (string.IsNullOrWhiteSpace(PurchasePriceInput))
            PurchasePriceInput = Math.Round(recommendedBuy, MidpointRounding.AwayFromZero).ToString("0");
    }

    private void RecalculateSaleOutcome()
    {
        if (!TryParseMoney(SoldPriceInput, out var soldPrice) ||
            !TryParseMoney(PurchasePriceInput, out var purchasePrice) ||
            soldPrice <= 0 || purchasePrice <= 0)
        {
            SaleOutcomeText = "Введите сумму продажи и закупки";
            SaleOutcomeBrush = Brushes.LightGray;
            return;
        }

        var fee = SelectedSaleChannel == "Продажа в Discord" ? DiscordSaleFee : AuctionSaleFee;
        var netIncome = soldPrice * (1 - fee);
        var profit = netIncome - purchasePrice;
        var profitPercent = purchasePrice == 0 ? 0 : profit / purchasePrice * 100;

        if (profit >= 0)
        {
            SaleOutcomeText = $"Профит: {FormatMoney(profit)} ({profitPercent:0.##}%)";
            SaleOutcomeBrush = new SolidColorBrush(Color.Parse("#44FF88"));
        }
        else
        {
            SaleOutcomeText = $"Просадка: {FormatMoney(Math.Abs(profit))} ({profitPercent:0.##}%)";
            SaleOutcomeBrush = new SolidColorBrush(Color.Parse("#FF6A78"));
        }
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

    private static bool TryParsePercent(string input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Replace("%", string.Empty, StringComparison.Ordinal).Replace(",", ".", StringComparison.Ordinal);
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatMoney(double value)
    {
        return $"{Math.Round(value, MidpointRounding.AwayFromZero):N0} ₽";
    }
}
