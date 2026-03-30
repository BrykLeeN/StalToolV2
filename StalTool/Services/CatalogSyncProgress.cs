namespace StalTool.Services;

public sealed record CatalogSyncProgress(int Percent, string Stage, string FileName, bool IsError = false);
