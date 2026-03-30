using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StalTool.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public event EventHandler? LoginSucceeded;

    [RelayCommand]
    private void SignIn()
    {
        LoginSucceeded?.Invoke(this, EventArgs.Empty);
    }
}
