using Avalonia;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StalTool;

sealed class Program
{
    private static readonly string CrashLogPath = Path.Combine(AppContext.BaseDirectory, "crash.log");

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                WriteCrashLog("AppDomain.UnhandledException", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            WriteCrashLog("Program.Main", ex);
            throw;
        }
    }
    
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void WriteCrashLog(string source, Exception ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}");
            sb.AppendLine(ex.ToString());
            sb.AppendLine(new string('-', 80));

            File.AppendAllText(CrashLogPath, sb.ToString());
        }
        catch
        {
            // Last-chance logging must never throw.
        }
    }
}
