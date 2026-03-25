using CommunityToolkit.Mvvm.ComponentModel;
using StalTool.ViewModels.Auction.Sections;

namespace StalTool.ViewModels.Auction;

public partial class AuctionViewModel : Base.ViewModelBase
{
    public AuctionViewModel()
    {
        PriceChartSection = new AuctionPriceChartViewModel();
        CurrentSection = PriceChartSection;
    }

    public AuctionPriceChartViewModel PriceChartSection { get; }

    [ObservableProperty]
    private Base.ViewModelBase? _currentSection;
}