using CommunityToolkit.Mvvm.ComponentModel;
using StalTool.ViewModels.Auction.Sections;

namespace StalTool.ViewModels.Auction;

public partial class AuctionViewModel : Base.ViewModelBase
{
    public AuctionViewModel()
    {
        PriceChartSection = new AuctionPriceChartViewModel();
        CalculatorSection = new AuctionCalculatorViewModel();
        RadarSection = new AuctionRadarViewModel();
        CurrentSection = PriceChartSection;
    }

    public AuctionPriceChartViewModel PriceChartSection { get; }
    public AuctionCalculatorViewModel CalculatorSection { get; }
    public AuctionRadarViewModel RadarSection { get; }

    [ObservableProperty]
    private Base.ViewModelBase? _currentSection;
}
