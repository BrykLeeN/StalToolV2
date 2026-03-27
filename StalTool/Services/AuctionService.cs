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

public class AuctionService
{
    private const string DefaultIcon = "avares://StalTool/Assets/appicons.png";
    private static readonly HttpClient Http = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Можно переопределить переменными окружения:
    // STALTOOL_GITHUB_OWNER, STALTOOL_GITHUB_REPO, STALTOOL_GITHUB_BRANCH, STALTOOL_STALCRAFT_REALM
    private readonly string _githubOwner = Environment.GetEnvironmentVariable("STALTOOL_GITHUB_OWNER") ?? "EXBO-Studio";
    private readonly string _githubRepo = Environment.GetEnvironmentVariable("STALTOOL_GITHUB_REPO") ?? "stalcraft-database";
    private readonly string _githubBranch = Environment.GetEnvironmentVariable("STALTOOL_GITHUB_BRANCH") ?? "main";
    private readonly string _stalcraftRealm = Environment.GetEnvironmentVariable("STALTOOL_STALCRAFT_REALM") ?? "ru";

    private readonly string _cacheDir;
    private readonly string _cacheItemsFile;
    private readonly string _cacheIconsDir;
    private readonly string _cacheRepoZipFile;
    private readonly string _cacheErrorLogFile;

    public AuctionService()
    {
        _cacheDir = GetSubjectCategoryDirectory();
        _cacheItemsFile = Path.Combine(_cacheDir, "items_catalog.json");
        _cacheIconsDir = Path.Combine(_cacheDir, "icons");
        _cacheRepoZipFile = Path.Combine(_cacheDir, "repo_snapshot.zip");
        _cacheErrorLogFile = Path.Combine(_cacheDir, "github_sync_error.log");
    }

    public ObservableCollection<AuctionCategoryGroup> GetCategoriesFromCacheOrMock()
    {
        var cached = LoadCategoriesFromCache();
        return cached.Count > 0 ? cached : GetMockCategories();
    }

    public async Task<ObservableCollection<AuctionCategoryGroup>> RefreshCategoriesFromGitHubAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_cacheDir);
            Directory.CreateDirectory(_cacheIconsDir);

            var zipBytes = await Http.GetByteArrayAsync(GetRepositoryZipUrl(), cancellationToken);
            await File.WriteAllBytesAsync(_cacheRepoZipFile, zipBytes, cancellationToken);

            using var zipMemory = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipMemory, ZipArchiveMode.Read);

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
            foreach (var entry in itemEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = entry.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync(cancellationToken);
                var relativePath = entry.FullName[(entry.FullName.IndexOf($"/{_stalcraftRealm}/items/", StringComparison.OrdinalIgnoreCase) + 1)..];
                var categoryFromFile = BuildCategoryNameFromPath(relativePath, $"{_stalcraftRealm}/items");
                parsedItems.AddRange(ParseItems(content, categoryFromFile));
            }

            if (parsedItems.Count == 0)
                return new ObservableCollection<AuctionCategoryGroup>();

            parsedItems = DeduplicateItems(parsedItems);

            foreach (var item in parsedItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                item.IconPath = await ResolveAndCacheIconFromArchiveAsync(item, iconById, cancellationToken);
            }

            SaveItemsCache(parsedItems);
            return BuildCategories(parsedItems);
        }
        catch (Exception ex)
        {
            TryWriteSyncError(ex);
            return new ObservableCollection<AuctionCategoryGroup>();
        }
    }

    public ObservableCollection<AuctionCategoryGroup> GetMockCategories()
    {
        return new ObservableCollection<AuctionCategoryGroup>
        {
            new()
            {
                CategoryName = "Оружие",
                Items = new ObservableCollection<AuctionCatalogItem>
                {
                    new() { ItemId = "ak74m", DisplayName = "АК-74М", Category = "Оружие", Rank = "epic", IconPath = DefaultIcon },
                    new() { ItemId = "svds", DisplayName = "СВДС", Category = "Оружие", Rank = "legendary", IconPath = DefaultIcon },
                    new() { ItemId = "ash12", DisplayName = "АШ-12", Category = "Оружие", Rank = "master", IconPath = DefaultIcon }
                }
            },
            new()
            {
                CategoryName = "Контейнеры",
                Items = new ObservableCollection<AuctionCatalogItem>
                {
                    new() { ItemId = "striker_case", DisplayName = "Ящик «Страйкер»", Category = "Контейнеры", Rank = "rare", IconPath = DefaultIcon },
                    new() { ItemId = "supply_box", DisplayName = "Снабженческий контейнер", Category = "Контейнеры", Rank = "common", IconPath = DefaultIcon }
                }
            },
            new()
            {
                CategoryName = "Броня",
                Items = new ObservableCollection<AuctionCatalogItem>
                {
                    new() { ItemId = "saturn", DisplayName = "Сатурн", Category = "Броня", Rank = "master", IconPath = DefaultIcon },
                    new() { ItemId = "skat9", DisplayName = "СКАТ-9", Category = "Броня", Rank = "epic", IconPath = DefaultIcon }
                }
            }
        };
    }

    public ObservableCollection<PricePoint> GetMockPriceBuffer(string itemId)
    {
        var now = DateTime.Now.Date;
        var seed = Math.Abs(itemId.GetHashCode(StringComparison.Ordinal));
        var basePrice = 1200 + (seed % 48000);
        var dayTrend = ((seed % 9) - 4) * 14.0;

        var points = new ObservableCollection<PricePoint>();
        const int daysInBuffer = 21;
        const int salesPerDay = 4;

        for (int dayOffset = daysInBuffer - 1; dayOffset >= 0; dayOffset--)
        {
            var date = now.AddDays(-dayOffset);
            var dayIndex = daysInBuffer - 1 - dayOffset;
            var seasonal = Math.Sin((dayIndex + (seed % 5)) / 3.5) * 90.0;
            var dayBase = basePrice + (dayTrend * dayIndex) + seasonal;

            for (int saleIndex = 0; saleIndex < salesPerDay; saleIndex++)
            {
                var hour = 9 + (saleIndex * 3);
                var variance = (saleIndex - 1.5) * 38;
                points.Add(new PricePoint
                {
                    Time = date.AddHours(hour),
                    Value = dayBase + variance
                });
            }
        }

        return points;
    }

    public ObservableCollection<AuctionLot> GetMockActiveLots(AuctionCatalogItem item)
    {
        var itemId = item.ItemId;
        var itemName = item.DisplayName;
        var seed = Math.Abs(itemId.GetHashCode(StringComparison.Ordinal));
        var basePrice = 1300 + (seed % 47000);
        var delta = Math.Max(35, basePrice * 0.018);

        return new ObservableCollection<AuctionLot>
        {
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                IconPath = string.IsNullOrWhiteSpace(item.IconPath) ? DefaultIcon : item.IconPath,
                Category = item.Category,
                Rank = item.Rank,
                Amount = 1,
                StartPrice = (long)Math.Round(basePrice - (delta * 0.9), 0),
                CurrentPrice = (long)Math.Round(basePrice - (delta * 0.4), 0),
                BuyoutPrice = (long)Math.Round(basePrice + (delta * 0.2), 0),
                StartTime = DateTime.Now.AddHours(-8),
                EndTime = DateTime.Now.AddHours(10),
                PriceStatus = "Низкая"
            },
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                IconPath = string.IsNullOrWhiteSpace(item.IconPath) ? DefaultIcon : item.IconPath,
                Category = item.Category,
                Rank = item.Rank,
                Amount = 1,
                StartPrice = (long)Math.Round(basePrice - (delta * 0.2), 0),
                CurrentPrice = (long)Math.Round(basePrice + (delta * 0.1), 0),
                BuyoutPrice = (long)Math.Round(basePrice + (delta * 0.45), 0),
                StartTime = DateTime.Now.AddHours(-4),
                EndTime = DateTime.Now.AddHours(15),
                PriceStatus = "Норма"
            },
            new()
            {
                ItemId = itemId,
                DisplayName = itemName,
                IconPath = string.IsNullOrWhiteSpace(item.IconPath) ? DefaultIcon : item.IconPath,
                Category = item.Category,
                Rank = item.Rank,
                Amount = 1,
                StartPrice = (long)Math.Round(basePrice + (delta * 0.55), 0),
                CurrentPrice = (long)Math.Round(basePrice + (delta * 0.9), 0),
                BuyoutPrice = (long)Math.Round(basePrice + (delta * 1.25), 0),
                StartTime = DateTime.Now.AddHours(-1),
                EndTime = DateTime.Now.AddHours(22),
                PriceStatus = "Высокая"
            }
        };
    }

    public PriceStats BuildStats(IEnumerable<PricePoint> points)
    {
        var list = points.ToList();
        if (list.Count == 0)
        {
            return new PriceStats
            {
                TrendText = "Нет данных",
                RecommendationText = "Ожидание данных"
            };
        }

        var min = (long)list.Min(x => x.Value);
        var max = (long)list.Max(x => x.Value);
        var avg = (long)list.Average(x => x.Value);

        var first = list.First().Value;
        var last = list.Last().Value;

        var changePercent = first == 0
            ? 0
            : ((last - first) / first) * 100.0;

        var trendText = changePercent switch
        {
            > 3 => "Рост",
            < -3 => "Падение",
            _ => "Боковик"
        };

        var recommendation = changePercent switch
        {
            < -5 => "Рассмотреть покупку",
            > 5 => "Рассмотреть продажу",
            _ => "Наблюдать"
        };

        return new PriceStats
        {
            MinPrice = min,
            MaxPrice = max,
            AveragePrice = avg,
            ChangePercent = Math.Round(changePercent, 2),
            TrendText = trendText,
            RecommendationText = recommendation
        };
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
                    Category = string.IsNullOrWhiteSpace(x.Category) ? "Без категории" : x.Category!,
                    Rank = string.IsNullOrWhiteSpace(x.Rank) ? "common" : x.Rank!,
                    IconPath = NormalizeIconPath(x.IconPath)
                })
                .ToList();

            items = DeduplicateItems(items);

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
            items.GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "Без категории" : x.Category)
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
            var category = obj["category"]?.GetValue<string>() ?? defaultCategory;
            items.AddRange(ParseItemArray(itemsArray, category));
            return items;
        }

        foreach (var kv in obj)
        {
            if (kv.Value is JsonArray categoryItems)
                items.AddRange(ParseItemArray(categoryItems, kv.Key));
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
            var category = entry["category"]?.GetValue<string>() ?? fallbackCategory;
            var rank = entry["rank"]?.GetValue<string>() ?? "common";
            var icon = entry["icon"]?.GetValue<string>()
                       ?? entry["iconUrl"]?.GetValue<string>()
                       ?? entry["image"]?.GetValue<string>();

            yield return new AuctionCatalogItem
            {
                ItemId = SlugifyOrFallback(itemId, name),
                DisplayName = name.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "Без категории" : category.Trim(),
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
        return firstSegment.Replace('_', ' ').Replace('-', ' ').Trim();
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
            Category = string.IsNullOrWhiteSpace(category) ? "Без категории" : category.Trim(),
            Rank = NormalizeRank(color),
            IconPath = string.Empty
        };
        return true;
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
        if (c.Contains("stalker"))
            return "rare";
        return "common";
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

    private static string GetSubjectCategoryDirectory()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            local = Path.Combine(home, ".local", "share");
        }

        return Path.Combine(local, "StalTool", "Subject_category");
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
        public string? ItemId { get; init; }
        public string? DisplayName { get; init; }
        public string? Category { get; init; }
        public string? Rank { get; init; }
        public string? IconPath { get; init; }
    }
}
