using System;

namespace StalTool.ViewModels.Base;

public class NavigationService
{
    public event Action<ViewModelBase?>? CurrentPageChanged;

    public ViewModelBase? CurrentPage { get; private set; }

    public void Navigate(ViewModelBase page)
    {
        CurrentPage = page;
        CurrentPageChanged?.Invoke(CurrentPage);
    }
}