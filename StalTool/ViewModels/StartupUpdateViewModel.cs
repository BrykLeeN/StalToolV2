using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using StalTool.Services;

namespace StalTool.ViewModels;

public partial class StartupUpdateViewModel : ObservableObject
{
    private readonly CatalogService _catalogService;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _stageText = "Подготовка";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentFileText))]
    [NotifyPropertyChangedFor(nameof(CurrentFileDisplayText))]
    private string _currentFileText = "Ожидание запуска";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorText = string.Empty;

    public ObservableCollection<string> InstalledFiles { get; } = new();
    public bool HasCurrentFileText => !string.IsNullOrWhiteSpace(CurrentFileDisplayText);
    public string CurrentFileDisplayText => IsLoggableFileName(CurrentFileText) ? string.Empty : CurrentFileText;

    public StartupUpdateViewModel(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task RunAsync()
    {
        var progress = new Progress<CatalogSyncProgress>(step =>
        {
            ProgressPercent = step.Percent;
            StageText = step.Stage;
            CurrentFileText = NormalizeCurrentFileText(step.Stage, step.FileName);

            if (IsLoggableFileName(CurrentFileText) &&
                InstalledFiles.All(x => !string.Equals(x, CurrentFileText, StringComparison.OrdinalIgnoreCase)))
            {
                InstalledFiles.Insert(0, CurrentFileText);
            }

            if (step.IsError)
            {
                HasError = true;
                ErrorText = step.FileName;
            }
        });

        await _catalogService.RefreshCategoriesFromGitHubAsync(progress);
    }

    private static string NormalizeCurrentFileText(string stage, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var trimmed = fileName.Trim();
        if (string.Equals(trimmed, stage?.Trim(), StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        if (IsOperationCaption(trimmed))
            return string.Empty;

        if (trimmed.Contains(Path.DirectorySeparatorChar) || trimmed.Contains(Path.AltDirectorySeparatorChar))
            return Path.GetFileName(trimmed);

        return trimmed;
    }

    private static bool IsOperationCaption(string value)
    {
        return value.Equals("Загрузка файлов", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Распаковка файлов", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Индексация удаленного каталога", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Переустановка не требуется", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Недостающие файлы установлены", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoggableFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var lowered = fileName.Trim().ToLowerInvariant();
        return lowered.EndsWith(".json") ||
               lowered.EndsWith(".png") ||
               lowered.EndsWith(".jpg") ||
               lowered.EndsWith(".jpeg") ||
               lowered.EndsWith(".webp") ||
               lowered.EndsWith(".bmp") ||
               lowered.EndsWith(".zip");
    }
}
