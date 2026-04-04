using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StalTool.Models;

public partial class AuctionCatalogItem : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rank { get; set; } = "common";
    public string IconPath { get; set; } = string.Empty;
    public bool HasQualityVariants { get; set; }
    public ObservableCollection<AuctionCatalogItem> QualityVariants { get; set; } = new();
    public IEnumerable<AuctionCatalogItem> VisibleQualityVariants => IsExpanded ? QualityVariants : Array.Empty<AuctionCatalogItem>();

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded;

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(VisibleQualityVariants));
    }
}
