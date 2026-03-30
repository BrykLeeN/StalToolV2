using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using StalTool.Services;
using StalTool.ViewModels;
using StalTool.Views;

namespace StalTool;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunStartupFlowAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task RunStartupFlowAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var updateViewModel = new StartupUpdateViewModel(new CatalogService());
        var updateWindow = new StartupUpdateWindow
        {
            DataContext = updateViewModel
        };
        desktop.MainWindow = updateWindow;
        updateWindow.Show();
        await updateViewModel.RunAsync();
        await Task.Delay(250);
        updateWindow.Close();

        var loginViewModel = new LoginViewModel();
        var loginWindow = new LoginWindow
        {
            DataContext = loginViewModel
        };

        var loginTask = new TaskCompletionSource<bool>();
        loginViewModel.LoginSucceeded += (_, _) => loginTask.TrySetResult(true);
        loginWindow.Closed += (_, _) => loginTask.TrySetResult(false);

        desktop.MainWindow = loginWindow;
        loginWindow.Show();

        var isAuthenticated = await loginTask.Task;
        if (!isAuthenticated)
        {
            desktop.Shutdown();
            return;
        }

        var mainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel()
        };
        mainWindow.Closed += (_, _) => desktop.Shutdown();

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        if (loginWindow.IsVisible)
            loginWindow.Close();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
