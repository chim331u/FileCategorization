using System.Collections.Immutable;
using FileCategorization_Web.Data.Caching;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using Fluxor;

namespace FileCategorization_Web.Features.FileManagement.Store;

[FeatureState]
public record FileState
{
    public ImmutableList<FilesDetailDto> Files { get; init; } = ImmutableList<FilesDetailDto>.Empty;
    public ImmutableList<string> Categories { get; init; } = ImmutableList<string>.Empty;
    public ImmutableList<ConfigsDto> Configurations { get; init; } = ImmutableList<ConfigsDto>.Empty;
    
    // UI State
    public bool IsLoading { get; init; } = false;
    public bool IsRefreshing { get; init; } = false;
    public bool IsTraining { get; init; } = false;
    public bool IsCategorizing { get; init; } = false;
    public string? Error { get; init; } = null;
    
    // Search and Filters
    public int SearchParameter { get; init; } = 3; // Default to "To Categorize"
    public string? SelectedCategory { get; init; } = null;
    
    // Console Output
    public ImmutableList<string> ConsoleMessages { get; init; } = ImmutableList<string>.Empty;
    
    // Cache State
    public CacheStatistics? CacheStatistics { get; init; } = null;
    public bool IsCacheWarming { get; init; } = false;
    public DateTime? LastCacheUpdate { get; init; } = null;
    
    private FileState() { }
    
    public static FileState InitialState => new();
}

public static class FileStateSelectors
{
    public static ImmutableList<FilesDetailDto> GetFilteredFiles(FileState state) =>
        state.SearchParameter switch
        {
            1 => state.Files, // All
            2 => state.Files.Where(f => !f.IsToCategorize).ToImmutableList(), // Categorized
            3 => state.Files.Where(f => f.IsToCategorize).ToImmutableList(), // To Categorize
            _ => state.Files
        };
    
    public static ImmutableList<FilesDetailDto> GetFilesToMove(FileState state) =>
        state.Files.Where(f => f.IsNotToMove).ToImmutableList();
    
    public static bool HasPendingOperations(FileState state) =>
        state.IsLoading || state.IsRefreshing || state.IsTraining;
}