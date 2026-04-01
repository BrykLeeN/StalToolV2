using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StalTool.Models;

namespace StalTool.Services;

public class CatalogService
{
    private const string DefaultIcon = "avares://StalTool/Assets/appicons.png";
    private static readonly HttpClient Http = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string[] ArtifactQualityOrder =
    {
        "common",
        "uncommon",
        "special",
        "rare",
        "exceptional",
        "legendary"
    };
    private static readonly Dictionary<string, string> ArtifactQualityDisplayName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["common"] = "Обычный",
        ["uncommon"] = "Необычный",
        ["special"] = "Особый",
        ["rare"] = "Редкий",
        ["exceptional"] = "Исключительный",
        ["legendary"] = "Легендарный"
    };
    private static readonly HashSet<string> ArtifactQualityTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "common", "uncommon", "special", "rare", "exceptional", "legendary", "epic", "master",
        "обычный", "необычный", "особый", "редкий", "исключительный", "легендарный",
        "серый", "зеленый", "зелёный", "синий", "розовый", "красный", "золотой", "желтый", "жёлтый"
    };
    private static readonly Dictionary<string, string> CategoryTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["armor"] = "Броня",
        ["artefact"] = "Артефакты",
        ["artifact"] = "Артефакты",
        ["attachment"] = "Модули",
        ["attachments"] = "Модули",
        ["backpack"] = "Рюкзаки",
        ["backpacks"] = "Рюкзаки",
        ["bullet"] = "Патроны",
        ["bullets"] = "Патроны",
        ["ammo"] = "Патроны",
        ["container"] = "Контейнеры",
        ["containers"] = "Контейнеры",
        ["case"] = "Контейнеры",
        ["cases"] = "Контейнеры",
        ["drink"] = "Напитки",
        ["drinks"] = "Напитки",
        ["food"] = "Еда",
        ["grenade"] = "Гранаты",
        ["grenades"] = "Гранаты",
        ["medicine"] = "Медицина",
        ["medical"] = "Медицина",
        ["medicines"] = "Медицина",
        ["medkit"] = "Медицина",
        ["medkits"] = "Медицина",
        ["misc"] = "Разное",
        ["miscellaneous"] = "Разное",
        ["other"] = "Разное",
        ["weapon"] = "Оружие",
        ["weapons"] = "Оружие",
        ["gun"] = "Оружие",
        ["guns"] = "Оружие",
        ["melee"] = "Ближний бой",
        ["resource"] = "Ресурсы",
        ["resources"] = "Ресурсы",
        ["consumables"] = "Расходники",
        ["consumable"] = "Расходники",
        ["parts"] = "Запчасти",
        ["part"] = "Запчасти",
        ["device"] = "Устройства",
        ["devices"] = "Устройства"
    };

    // Можно переопределить переменными окружения:
    // STALTOOL_GITHUB_OWNER, STALTOOL_GITHUB_REPO, STALTOOL_GITHUB_BRANCH, STALTOOL_STALCRAFT_REALM
    private readonly string _githubOwner = Environment.GetEnvironmentVariable("STALTOOL_GITHUB_OWNER") ?? "EXBO-Studio";
    private readonly string _githubRepo = Environment.GetEnvironmentVariable("STALTOOL_GITHUB_REPO") ?? "stalcraft-database";
    private readonly string _githubBranch = Environment.GetEnvironmentVariable("STALTOOL_GITHUB_BRANCH") ?? "main";
    private readonly string _stalcraftRealm = Environment.GetEnvironmentVariable("STALTOOL_STALCRAFT_REALM") ?? "ru";

    private readonly string _cacheDir;
    private readonly string _appCacheRootDir;
    private readonly string _cacheItemsFile;
    private readonly string _cacheIconsDir;
    private readonly string _cacheRepoZipFile;
    private readonly string _cacheErrorLogFile;
    private readonly string _cacheSyncStateFile;

    public CatalogService()
    {
        _appCacheRootDir = GetAppCacheRootDirectory();
        _cacheDir = Path.Combine(_appCacheRootDir, "Subject_category");
        _cacheItemsFile = Path.Combine(_cacheDir, "items_catalog.json");
        _cacheIconsDir = Path.Combine(_cacheDir, "icons");
        _cacheRepoZipFile = Path.Combine(_cacheDir, "repo_snapshot.zip");
        _cacheErrorLogFile = Path.Combine(_cacheDir, "github_sync_error.log");
        _cacheSyncStateFile = Path.Combine(_cacheDir, "catalog_sync_state.json");
    }

    public ObservableCollection<AuctionCategoryGroup> GetCachedCategories()
    {
        return LoadCategoriesFromCache();
    }

    public async Task<ObservableCollection<AuctionCategoryGroup>> RefreshCategoriesOnScheduleAsync(
        IProgress<CatalogSyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var scheduleState = LoadSyncState();
        var cached = LoadCategoriesFromCache();
        var hasValidCache = HasValidLocalCatalogFolder() && cached.Count > 0;
        var hasDueSlot = TryGetLatestDueScheduleSlot(now, out var latestDueSlot);

        if (!hasValidCache)
        {
            progress?.Report(new CatalogSyncProgress(8, "Установка каталога", "Первичный запуск"));
            var installed = await RefreshCategoriesFromGitHubAsync(progress, cancellationToken, forceReinstall: true);
            var effective = installed.Count > 0 ? installed : LoadCategoriesFromCache();

            if (effective.Count > 0)
            {
                scheduleState.LastSuccessfulSyncUtc = DateTimeOffset.UtcNow;
                if (hasDueSlot)
                    scheduleState.LastCheckedScheduledSlotUtc = latestDueSlot.ToUniversalTime();
                scheduleState.LastRemoteCommitSha = await TryGetRemoteHeadCommitShaAsync(cancellationToken);
                SaveSyncState(scheduleState);
            }

            return effective;
        }

        if (!hasDueSlot || IsSlotAlreadyChecked(scheduleState, latestDueSlot))
        {
            progress?.Report(new CatalogSyncProgress(100, "Локальный каталог актуален", "Переустановка не требуется"));
            return cached;
        }

        progress?.Report(new CatalogSyncProgress(10, "Идет проверка обновлений, подождите", "Проверка GitHub"));
        var remoteSha = await TryGetRemoteHeadCommitShaAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(remoteSha))
        {
            progress?.Report(new CatalogSyncProgress(100, "Проверка GitHub недоступна", "Повтор при следующем запуске"));
            return cached;
        }

        if (string.Equals(scheduleState.LastRemoteCommitSha, remoteSha, StringComparison.OrdinalIgnoreCase))
        {
            scheduleState.LastCheckedScheduledSlotUtc = latestDueSlot.ToUniversalTime();
            SaveSyncState(scheduleState);
            progress?.Report(new CatalogSyncProgress(100, "Обновлений не найдено", "Локальный каталог актуален"));
            return cached;
        }

        progress?.Report(new CatalogSyncProgress(14, "Начинается обновление, подождите", "Подготовка загрузки"));
        var refreshed = await RefreshCategoriesFromGitHubAsync(progress, cancellationToken, forceReinstall: true);
        var refreshedEffective = refreshed.Count > 0 ? refreshed : LoadCategoriesFromCache();

        if (refreshedEffective.Count > 0)
        {
            scheduleState.LastRemoteCommitSha = remoteSha;
            scheduleState.LastCheckedScheduledSlotUtc = latestDueSlot.ToUniversalTime();
            scheduleState.LastSuccessfulSyncUtc = DateTimeOffset.UtcNow;
            SaveSyncState(scheduleState);
        }

        return refreshedEffective;
    }

    public async Task<ObservableCollection<AuctionCategoryGroup>> RefreshCategoriesFromGitHubAsync(
        IProgress<CatalogSyncProgress>? progress = null,
        CancellationToken cancellationToken = default,
        bool forceReinstall = false)
    {
        try
        {
            progress?.Report(new CatalogSyncProgress(2, "Проверка локальных файлов", _cacheDir));

            var cachedRawItems = LoadCachedItemsRaw();
            var cached = LoadCategoriesFromCache();
            var missingIconIds = GetMissingOrCorruptedIconIds(cachedRawItems);

            if (!forceReinstall && cachedRawItems.Count > 0 && cached.Count > 0 && missingIconIds.Count == 0)
            {
                progress?.Report(new CatalogSyncProgress(100, "Локальный каталог актуален", "Переустановка не требуется"));
                return cached;
            }

            if (!forceReinstall && cachedRawItems.Count > 0 && cached.Count > 0 && missingIconIds.Count > 0)
            {
                progress?.Report(new CatalogSyncProgress(10, "Восстановление каталога", $"{missingIconIds.Count} файлов"));
                await RepairMissingIconsAsync(missingIconIds, cachedRawItems, progress, cancellationToken);
                progress?.Report(new CatalogSyncProgress(100, "Каталог восстановлен", "Недостающие файлы установлены"));
                return LoadCategoriesFromCache();
            }

            progress?.Report(new CatalogSyncProgress(8, "Установка каталога", "Подготовка директорий"));
            Directory.CreateDirectory(_cacheDir);
            Directory.CreateDirectory(_cacheIconsDir);

            if (!File.Exists(_cacheRepoZipFile))
            {
                progress?.Report(new CatalogSyncProgress(16, "Загрузка архива каталога", "Загрузка файлов"));
                var zipBytes = await Http.GetByteArrayAsync(GetRepositoryZipUrl(), cancellationToken);
                await File.WriteAllBytesAsync(_cacheRepoZipFile, zipBytes, cancellationToken);
            }
            else
            {
                progress?.Report(new CatalogSyncProgress(16, "Использование локального архива", "Загрузка файлов"));
            }

            progress?.Report(new CatalogSyncProgress(24, "Распаковка файлов каталога", "Распаковка файлов"));
            await using var zipStream = File.OpenRead(_cacheRepoZipFile);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var itemsMarker = $"/{_stalcraftRealm}/items/";
            var iconsMarker = $"/{_stalcraftRealm}/icons/";

            var itemEntries = archive.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) &&
                            e.FullName.Contains(itemsMarker, StringComparison.OrdinalIgnoreCase) &&
                            e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var iconEntries = archive.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) &&
                            e.FullName.Contains(iconsMarker, StringComparison.OrdinalIgnoreCase) &&
                            IsImageFile(e.Name))
                .ToList();

            if (itemEntries.Count == 0)
                return new ObservableCollection<AuctionCategoryGroup>();

            var iconById = iconEntries
                .GroupBy(x => Path.GetFileNameWithoutExtension(x.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var parsedItems = new List<AuctionCatalogItem>();
            for (var i = 0; i < itemEntries.Count; i++)
            {
                var entry = itemEntries[i];
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = entry.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync(cancellationToken);
                var relativePath = entry.FullName[(entry.FullName.IndexOf($"/{_stalcraftRealm}/items/", StringComparison.OrdinalIgnoreCase) + 1)..];
                var categoryFromFile = BuildCategoryNameFromPath(relativePath, $"{_stalcraftRealm}/items");
                parsedItems.AddRange(ParseItems(content, categoryFromFile));
                var parsePercent = 24 + ((i + 1) * 36 / Math.Max(1, itemEntries.Count));
                progress?.Report(new CatalogSyncProgress(parsePercent, "Установка каталога", entry.Name));
            }

            if (parsedItems.Count == 0)
                return new ObservableCollection<AuctionCategoryGroup>();

            parsedItems = DeduplicateItems(parsedItems);

            for (var i = 0; i < parsedItems.Count; i++)
            {
                var item = parsedItems[i];
                cancellationToken.ThrowIfCancellationRequested();
                item.IconPath = await ResolveAndCacheIconFromArchiveAsync(item, iconById, cancellationToken);
                if (i % 6 == 0 || i == parsedItems.Count - 1)
                {
                    var iconPercent = 62 + ((i + 1) * 34 / Math.Max(1, parsedItems.Count));
                    progress?.Report(new CatalogSyncProgress(iconPercent, "Установка иконок", item.DisplayName));
                }
            }

            SaveItemsCache(parsedItems);

            if (File.Exists(_cacheRepoZipFile))
                File.Delete(_cacheRepoZipFile);

            progress?.Report(new CatalogSyncProgress(100, "Каталог обновлен", "items_catalog.json"));
            return BuildCategories(ExpandArtifactQualities(parsedItems));
        }
        catch (Exception ex)
        {
            TryWriteSyncError(ex);
            progress?.Report(new CatalogSyncProgress(100, "Ошибка обновления каталога", ex.Message, true));
            return LoadCategoriesFromCache();
        }
        finally
        {
            try
            {
                if (File.Exists(_cacheRepoZipFile))
                    File.Delete(_cacheRepoZipFile);
            }
            catch
            {
                // ignored
            }
        }
    }

    private List<CachedCatalogItem> LoadCachedItemsRaw()
    {
        try
        {
            if (!File.Exists(_cacheItemsFile))
                return new List<CachedCatalogItem>();

            var json = File.ReadAllText(_cacheItemsFile, Encoding.UTF8);
            return JsonSerializer.Deserialize<List<CachedCatalogItem>>(json) ?? new List<CachedCatalogItem>();
        }
        catch
        {
            return new List<CachedCatalogItem>();
        }
    }

    private List<string> GetMissingOrCorruptedIconIds(List<CachedCatalogItem> cachedRawItems)
    {
        if (cachedRawItems.Count == 0)
            return new List<string>();

        var missing = new List<string>();
        foreach (var item in cachedRawItems)
        {
            if (string.IsNullOrWhiteSpace(item.ItemId))
                continue;
            if (string.IsNullOrWhiteSpace(item.IconPath))
            {
                missing.Add(item.ItemId!);
                continue;
            }
            if (!Uri.TryCreate(item.IconPath, UriKind.Absolute, out var iconUri) || iconUri.Scheme != Uri.UriSchemeFile)
                continue;

            var localPath = iconUri.LocalPath;
            if (!localPath.StartsWith(_cacheIconsDir, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsValidCachedIconFile(localPath))
                missing.Add(item.ItemId!);
        }

        return missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task RepairMissingIconsAsync(
        List<string> missingIconIds,
        List<CachedCatalogItem> cachedRawItems,
        IProgress<CatalogSyncProgress>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_cacheIconsDir);
        progress?.Report(new CatalogSyncProgress(12, "Проверка архива иконок", "Индексация удаленного каталога"));
        var iconEntries = await LoadGitHubDirectoryRecursiveAsync($"{_stalcraftRealm}/icons", cancellationToken);
        var iconById = iconEntries
            .Where(x => IsImageFile(x.Name) && !string.IsNullOrWhiteSpace(x.DownloadUrl))
            .GroupBy(x => Path.GetFileNameWithoutExtension(x.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < missingIconIds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var itemId = missingIconIds[i];
            if (!iconById.TryGetValue(itemId, out var entry) || string.IsNullOrWhiteSpace(entry.DownloadUrl))
                continue;

            var ext = Path.GetExtension(entry.Name);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".png";
            var targetPath = Path.Combine(_cacheIconsDir, $"{itemId}{ext}");

            var bytes = await Http.GetByteArrayAsync(entry.DownloadUrl, cancellationToken);
            await File.WriteAllBytesAsync(targetPath, bytes, cancellationToken);

            foreach (var raw in cachedRawItems.Where(x => string.Equals(x.ItemId, itemId, StringComparison.OrdinalIgnoreCase)))
                raw.IconPath = new Uri(targetPath).AbsoluteUri;

            var percent = 16 + ((i + 1) * 82 / Math.Max(1, missingIconIds.Count));
            progress?.Report(new CatalogSyncProgress(percent, "Восстановление каталога", Path.GetFileName(targetPath)));
        }

        SaveCachedRawItems(cachedRawItems);
    }

    private static bool IsValidCachedIconFile(string path)
    {
        if (!File.Exists(path))
            return false;

        if (!IsImageFile(path))
            return false;

        try
        {
            var info = new FileInfo(path);
            return info.Length > 16;
        }
        catch
        {
            return false;
        }
    }

    private void SaveCachedRawItems(List<CachedCatalogItem> items)
    {
        Directory.CreateDirectory(_cacheDir);
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(_cacheItemsFile, json, Encoding.UTF8);
    }

    private ObservableCollection<AuctionCategoryGroup> LoadCategoriesFromCache()
    {
        try
        {
            if (!File.Exists(_cacheItemsFile))
                return new ObservableCollection<AuctionCategoryGroup>();

            var json = File.ReadAllText(_cacheItemsFile, Encoding.UTF8);
            var cachedItems = JsonSerializer.Deserialize<List<CachedCatalogItem>>(json) ?? new List<CachedCatalogItem>();
            var items = cachedItems
                .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
                .Select(x => new AuctionCatalogItem
                {
                    ItemId = string.IsNullOrWhiteSpace(x.ItemId) ? Slugify(x.DisplayName!) : x.ItemId!,
                    DisplayName = x.DisplayName!,
                    Category = NormalizeCategory(x.Category),
                    Rank = string.IsNullOrWhiteSpace(x.Rank) ? "common" : x.Rank!,
                    IconPath = NormalizeIconPath(x.IconPath)
                })
                .ToList();

            items = DeduplicateItems(items);
            items = ExpandArtifactQualities(items);

            return BuildCategories(items);
        }
        catch
        {
            return new ObservableCollection<AuctionCategoryGroup>();
        }
    }

    private void SaveItemsCache(List<AuctionCatalogItem> items)
    {
        var normalized = items
            .Select(x => new CachedCatalogItem
            {
                ItemId = x.ItemId,
                DisplayName = x.DisplayName,
                Category = x.Category,
                Rank = x.Rank,
                IconPath = x.IconPath
            })
            .OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Directory.CreateDirectory(_cacheDir);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        File.WriteAllText(_cacheItemsFile, json, Encoding.UTF8);
    }

    private static ObservableCollection<AuctionCategoryGroup> BuildCategories(IEnumerable<AuctionCatalogItem> items)
    {
        return new ObservableCollection<AuctionCategoryGroup>(
            items.GroupBy(x => NormalizeCategory(x.Category))
                .OrderBy(g => g.Key, StringComparer.Create(new CultureInfo("ru-RU"), ignoreCase: true))
                .Select(g => new AuctionCategoryGroup
                {
                    CategoryName = g.Key,
                    Items = new ObservableCollection<AuctionCatalogItem>(
                        g.OrderBy(x => x.DisplayName, StringComparer.Create(new CultureInfo("ru-RU"), ignoreCase: true)))
                }));
    }

    private async Task<List<GithubContentEntry>> LoadGitHubDirectoryRecursiveAsync(string path, CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_githubOwner}/{_githubRepo}/contents/{Uri.EscapeDataString(path)}?ref={Uri.EscapeDataString(_githubBranch)}";
        var json = await Http.GetStringAsync(url, cancellationToken);
        var node = JsonNode.Parse(json);
        var result = new List<GithubContentEntry>();

        if (node is not JsonArray array)
            return result;

        foreach (var element in array.OfType<JsonObject>())
        {
            var type = element["type"]?.GetValue<string>() ?? string.Empty;
            var entryPath = element["path"]?.GetValue<string>() ?? string.Empty;
            var name = element["name"]?.GetValue<string>() ?? string.Empty;
            var downloadUrl = element["download_url"]?.GetValue<string>();

            if (string.Equals(type, "dir", StringComparison.OrdinalIgnoreCase))
            {
                var nested = await LoadGitHubDirectoryRecursiveAsync(entryPath, cancellationToken);
                result.AddRange(nested);
                continue;
            }

            result.Add(new GithubContentEntry
            {
                Type = "file",
                Path = entryPath,
                Name = name,
                DownloadUrl = downloadUrl
            });
        }

        return result;
    }

    private List<AuctionCatalogItem> ParseItems(string json, string defaultCategory)
    {
        var node = JsonNode.Parse(json);
        var items = new List<AuctionCatalogItem>();

        if (node is JsonArray array)
        {
            items.AddRange(ParseItemArray(array, defaultCategory));
            return items;
        }

        if (node is not JsonObject obj)
            return items;

        if (TryParseStalcraftItemObject(obj, defaultCategory, out var stalcraftItem))
        {
            items.Add(stalcraftItem);
            return items;
        }

        if (obj["items"] is JsonArray itemsArray)
        {
            var category = NormalizeCategory(obj["category"]?.GetValue<string>() ?? defaultCategory);
            items.AddRange(ParseItemArray(itemsArray, category));
            return items;
        }

        foreach (var kv in obj)
        {
            if (kv.Value is JsonArray categoryItems)
                items.AddRange(ParseItemArray(categoryItems, NormalizeCategory(kv.Key)));
        }

        return items;
    }

    private IEnumerable<AuctionCatalogItem> ParseItemArray(JsonArray array, string fallbackCategory)
    {
        foreach (var entry in array.OfType<JsonObject>())
        {
            var name = entry["name"]?.GetValue<string>()
                       ?? entry["displayName"]?.GetValue<string>()
                       ?? entry["title"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var itemId = entry["itemId"]?.GetValue<string>()
                         ?? entry["id"]?.GetValue<string>()
                         ?? Slugify(name);
            var category = NormalizeCategory(entry["category"]?.GetValue<string>() ?? fallbackCategory);
            var rank = entry["rank"]?.GetValue<string>() ?? "common";
            var icon = entry["icon"]?.GetValue<string>()
                       ?? entry["iconUrl"]?.GetValue<string>()
                       ?? entry["image"]?.GetValue<string>();

            yield return new AuctionCatalogItem
            {
                ItemId = SlugifyOrFallback(itemId, name),
                DisplayName = name.Trim(),
                Category = NormalizeCategory(category),
                Rank = string.IsNullOrWhiteSpace(rank) ? "common" : rank.Trim().ToLowerInvariant(),
                IconPath = icon?.Trim() ?? string.Empty
            };
        }
    }

    private async Task<string> ResolveAndCacheIconAsync(
        AuctionCatalogItem item,
        Dictionary<string, string> imageByName,
        CancellationToken cancellationToken)
    {
        var directIcon = item.IconPath;
        string? downloadUrl = null;

        if (!string.IsNullOrWhiteSpace(directIcon))
        {
            if (Uri.TryCreate(directIcon, UriKind.Absolute, out var iconUri) &&
                (iconUri.Scheme == Uri.UriSchemeHttp || iconUri.Scheme == Uri.UriSchemeHttps))
            {
                downloadUrl = directIcon;
            }
            else if (IsImageFile(directIcon))
            {
                var relative = directIcon.TrimStart('/');
                downloadUrl = BuildRawGitHubUrl(relative);
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            var candidates = new[]
            {
                item.ItemId,
                item.ItemId.ToLowerInvariant(),
                Slugify(item.DisplayName),
                Slugify(item.DisplayName).ToLowerInvariant()
            };

            foreach (var key in candidates)
            {
                if (imageByName.TryGetValue(key, out var found))
                {
                    downloadUrl = found;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
            return DefaultIcon;

        try
        {
            Directory.CreateDirectory(_cacheIconsDir);
            var extension = GetExtensionFromUrl(downloadUrl);
            var targetPath = Path.Combine(_cacheIconsDir, $"{item.ItemId}{extension}");
            var bytes = await Http.GetByteArrayAsync(downloadUrl, cancellationToken);
            await File.WriteAllBytesAsync(targetPath, bytes, cancellationToken);
            return new Uri(targetPath).AbsoluteUri;
        }
        catch
        {
            return DefaultIcon;
        }
    }

    private async Task<string> ResolveAndCacheIconFromArchiveAsync(
        AuctionCatalogItem item,
        Dictionary<string, ZipArchiveEntry> iconById,
        CancellationToken cancellationToken)
    {
        if (!iconById.TryGetValue(item.ItemId, out var entry))
            return DefaultIcon;

        try
        {
            Directory.CreateDirectory(_cacheIconsDir);
            var ext = Path.GetExtension(entry.Name);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".png";
            var targetPath = Path.Combine(_cacheIconsDir, $"{item.ItemId}{ext}");

            await using var source = entry.Open();
            await using var target = File.Create(targetPath);
            await source.CopyToAsync(target, cancellationToken);
            return new Uri(targetPath).AbsoluteUri;
        }
        catch
        {
            return DefaultIcon;
        }
    }

    private string BuildRawGitHubUrl(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return $"https://raw.githubusercontent.com/{_githubOwner}/{_githubRepo}/{_githubBranch}/{normalized}";
    }

    private static string NormalizeIconPath(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
            return DefaultIcon;

        if (Uri.TryCreate(iconPath, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (Path.IsPathRooted(iconPath))
            return new Uri(iconPath).AbsoluteUri;

        return DefaultIcon;
    }

    private static bool IsImageFile(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp";
    }

    private static string GetExtensionFromUrl(string url)
    {
        try
        {
            var ext = Path.GetExtension(new Uri(url).AbsolutePath);
            return string.IsNullOrWhiteSpace(ext) ? ".png" : ext;
        }
        catch
        {
            return ".png";
        }
    }

    private static string BuildCategoryNameFromPath(string filePath, string itemsRoot)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var root = itemsRoot.Trim('/').Replace('\\', '/');
        var relative = normalizedPath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)
            ? normalizedPath[(root.Length + 1)..]
            : normalizedPath;

        var dir = Path.GetDirectoryName(relative)?.Replace('\\', '/');
        var raw = string.IsNullOrWhiteSpace(dir) ? Path.GetFileNameWithoutExtension(relative) : dir;
        if (string.IsNullOrWhiteSpace(raw))
            return "Без категории";

        var firstSegment = raw.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? raw;
        return NormalizeCategory(firstSegment.Replace('_', ' ').Replace('-', ' ').Trim());
    }

    private static bool TryParseStalcraftItemObject(JsonObject obj, string defaultCategory, out AuctionCatalogItem item)
    {
        item = null!;
        var id = obj["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id))
            return false;

        string? displayName = null;
        if (obj["name"] is JsonObject nameObj)
        {
            if (nameObj["lines"] is JsonObject linesObj)
                displayName = linesObj["ru"]?.GetValue<string>() ?? linesObj["en"]?.GetValue<string>();

            displayName ??= nameObj["key"]?.GetValue<string>();
        }

        displayName ??= id;
        var categoryRaw = obj["category"]?.GetValue<string>();
        var category = !string.IsNullOrWhiteSpace(categoryRaw)
            ? categoryRaw.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? defaultCategory
            : defaultCategory;
        var color = obj["color"]?.GetValue<string>() ?? "common";

        item = new AuctionCatalogItem
        {
            ItemId = id,
            DisplayName = displayName.Trim(),
            Category = NormalizeCategory(category),
            Rank = NormalizeRank(color),
            IconPath = string.Empty
        };
        return true;
    }

    private static string NormalizeCategory(string? rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
            return "Без категории";

        var trimmed = rawCategory.Trim();
        if (CategoryTranslations.TryGetValue(trimmed, out var direct))
            return direct;

        var normalized = Regex.Replace(trimmed.ToLowerInvariant(), @"[^a-zа-яё0-9]+", " ").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "Без категории";

        if (CategoryTranslations.TryGetValue(normalized, out var translated))
            return translated;

        return trimmed;
    }

    private static string NormalizeRank(string color)
    {
        var c = color.Trim().ToLowerInvariant();
        if (c.Contains("master"))
            return "master";
        if (c.Contains("legend"))
            return "legendary";
        if (c.Contains("veteran"))
            return "epic";
        if (c.Contains("uncommon") || c.Contains("newbie"))
            return "common";
        if (c.Contains("stalker"))
            return "rare";
        return "common";
    }

    private static List<AuctionCatalogItem> ExpandArtifactQualities(List<AuctionCatalogItem> items)
    {
        var artifacts = items
            .Where(x => string.Equals(x.Category, "Артефакты", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (artifacts.Count == 0)
            return items;

        var notArtifacts = items
            .Where(x => !string.Equals(x.Category, "Артефакты", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var expandedArtifacts = artifacts
            .GroupBy(GetArtifactBaseKey, StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(group =>
            {
                var representative = group
                    .OrderByDescending(x => !string.IsNullOrWhiteSpace(x.IconPath))
                    .ThenByDescending(x => x.DisplayName.Length)
                    .First();

                var baseName = GetArtifactBaseName(representative);
                var baseId = SlugifyOrFallback(GetArtifactBaseId(representative), representative.ItemId);
                var iconByRank = group
                    .GroupBy(x => NormalizeArtifactQualityRank(x.Rank), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.IconPath))
                            .Select(x => x.IconPath)
                            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? DefaultIcon,
                        StringComparer.OrdinalIgnoreCase);

                var qualityItems = ArtifactQualityOrder.Select(rank => new AuctionCatalogItem
                {
                    ItemId = $"{baseId}_{rank}",
                    DisplayName = $"{baseName} - {ArtifactQualityDisplayName[rank]}",
                    Category = "Артефакты",
                    Rank = ToArtifactRankKey(rank),
                    IconPath = iconByRank.TryGetValue(rank, out var icon) ? icon : NormalizeIconPath(representative.IconPath)
                }).ToList();

                return new AuctionCatalogItem
                {
                    ItemId = baseId,
                    DisplayName = baseName,
                    Category = "Артефакты",
                    Rank = string.Empty,
                    IconPath = NormalizeIconPath(representative.IconPath),
                    HasQualityVariants = true,
                    QualityVariants = new ObservableCollection<AuctionCatalogItem>(qualityItems)
                };
            })
            .ToList();

        notArtifacts.AddRange(expandedArtifacts);
        return notArtifacts;
    }

    private static string GetArtifactBaseKey(AuctionCatalogItem item)
    {
        var byId = GetArtifactBaseId(item);
        if (!string.IsNullOrWhiteSpace(byId))
            return byId;

        return Slugify(GetArtifactBaseName(item));
    }

    private static string GetArtifactBaseId(AuctionCatalogItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ItemId))
            return string.Empty;

        var id = item.ItemId.Trim();
        var parts = id.Split('_', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count == 0)
            return id;

        while (parts.Count > 0 && ArtifactQualityTokens.Contains(parts[^1]))
            parts.RemoveAt(parts.Count - 1);

        return parts.Count == 0 ? id : string.Join('_', parts);
    }

    private static string GetArtifactBaseName(AuctionCatalogItem item)
    {
        if (string.IsNullOrWhiteSpace(item.DisplayName))
            return item.ItemId;

        var words = Regex.Split(item.DisplayName.Trim(), @"[\s\-_()\[\]/]+")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (words.Count == 0)
            return item.DisplayName.Trim();

        while (words.Count > 0 && ArtifactQualityTokens.Contains(words[^1]))
            words.RemoveAt(words.Count - 1);

        var result = string.Join(' ', words).Trim();
        return string.IsNullOrWhiteSpace(result) ? item.DisplayName.Trim() : result;
    }

    private static string NormalizeArtifactQualityRank(string? rank)
    {
        var normalized = (rank ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "master" => "exceptional",
            "epic" => "rare",
            "rare" => "special",
            _ => normalized
        };
    }

    private static string ToArtifactRankKey(string rank)
    {
        return $"artifact_{rank}";
    }

    private static List<AuctionCatalogItem> DeduplicateItems(IEnumerable<AuctionCatalogItem> items)
    {
        return items
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId) && !string.IsNullOrWhiteSpace(x.DisplayName))
            .GroupBy(x => x.ItemId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                // Берем самую "полную" запись: с иконкой, категорией и более информативным именем.
                return g.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.IconPath))
                    .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.Category))
                    .ThenByDescending(x => x.DisplayName.Length)
                    .First();
            })
            .ToList();
    }

    private static string SlugifyOrFallback(string? value, string fallbackName)
    {
        var normalized = Slugify(value);
        return string.IsNullOrWhiteSpace(normalized) ? Slugify(fallbackName) : normalized;
    }

    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lower = value.Trim().ToLowerInvariant();
        var cleaned = Regex.Replace(lower, @"[^a-zа-яё0-9]+", "_");
        cleaned = Regex.Replace(cleaned, "_{2,}", "_").Trim('_');
        return string.IsNullOrWhiteSpace(cleaned) ? "item" : cleaned;
    }

    private bool HasValidLocalCatalogFolder()
    {
        if (!Directory.Exists(_appCacheRootDir) || !Directory.Exists(_cacheDir))
            return false;

        if (!File.Exists(_cacheItemsFile))
            return false;

        var hasAnyIcon = Directory.Exists(_cacheIconsDir) &&
                         Directory.EnumerateFiles(_cacheIconsDir, "*", SearchOption.TopDirectoryOnly).Any();

        return hasAnyIcon;
    }

    private async Task<string?> TryGetRemoteHeadCommitShaAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.github.com/repos/{_githubOwner}/{_githubRepo}/branches/{Uri.EscapeDataString(_githubBranch)}";
            var json = await Http.GetStringAsync(url, cancellationToken);
            var root = JsonNode.Parse(json);
            return root?["commit"]?["sha"]?.GetValue<string>()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private CatalogSyncState LoadSyncState()
    {
        try
        {
            if (!File.Exists(_cacheSyncStateFile))
                return new CatalogSyncState();

            var json = File.ReadAllText(_cacheSyncStateFile, Encoding.UTF8);
            return JsonSerializer.Deserialize<CatalogSyncState>(json) ?? new CatalogSyncState();
        }
        catch
        {
            return new CatalogSyncState();
        }
    }

    private void SaveSyncState(CatalogSyncState state)
    {
        Directory.CreateDirectory(_cacheDir);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_cacheSyncStateFile, json, Encoding.UTF8);
    }

    private static bool IsSlotAlreadyChecked(CatalogSyncState state, DateTimeOffset dueSlotLocal)
    {
        if (!state.LastCheckedScheduledSlotUtc.HasValue)
            return false;

        return state.LastCheckedScheduledSlotUtc.Value >= dueSlotLocal.ToUniversalTime();
    }

    private static bool TryGetLatestDueScheduleSlot(DateTimeOffset nowLocal, out DateTimeOffset dueSlotLocal)
    {
        dueSlotLocal = default;
        var latest = DateTimeOffset.MinValue;
        var hasValue = false;
        var startOfWeek = GetWeekStartMonday(nowLocal.Date);

        for (var weekOffset = -2; weekOffset <= 0; weekOffset++)
        {
            var weekStart = startOfWeek.AddDays(7 * weekOffset);
            var wednesday = weekStart.AddDays(2);

            var noon = new DateTimeOffset(new DateTime(
                wednesday.Year, wednesday.Month, wednesday.Day, 12, 0, 0, DateTimeKind.Local));
            var evening = new DateTimeOffset(new DateTime(
                wednesday.Year, wednesday.Month, wednesday.Day, 20, 0, 0, DateTimeKind.Local));

            if (noon <= nowLocal && (!hasValue || noon > latest))
            {
                latest = noon;
                hasValue = true;
            }

            if (evening <= nowLocal && (!hasValue || evening > latest))
            {
                latest = evening;
                hasValue = true;
            }
        }

        if (!hasValue)
            return false;

        dueSlotLocal = latest;
        return true;
    }

    private static DateTime GetWeekStartMonday(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var shift = (dayOfWeek + 6) % 7;
        return date.Date.AddDays(-shift);
    }

    private static string GetAppCacheRootDirectory()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            local = Path.Combine(home, ".local", "share");
        }

        return Path.Combine(local, "StalTool");
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("StalTool/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private string GetRepositoryZipUrl()
    {
        return $"https://codeload.github.com/{_githubOwner}/{_githubRepo}/zip/refs/heads/{_githubBranch}";
    }

    private void TryWriteSyncError(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(_cacheDir);
            var text = $"[{DateTime.Now:O}] {ex}\n\n";
            File.AppendAllText(_cacheErrorLogFile, text, Encoding.UTF8);
        }
        catch
        {
            // ignored
        }
    }

    private sealed class GithubContentEntry
    {
        public string Type { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? DownloadUrl { get; init; }
    }

    private sealed class CachedCatalogItem
    {
        public string? ItemId { get; set; }
        public string? DisplayName { get; set; }
        public string? Category { get; set; }
        public string? Rank { get; set; }
        public string? IconPath { get; set; }
    }

    private sealed class CatalogSyncState
    {
        public DateTimeOffset? LastCheckedScheduledSlotUtc { get; set; }
        public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }
        public string? LastRemoteCommitSha { get; set; }
    }
}
