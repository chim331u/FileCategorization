namespace FileCategorization_Web.Data.Caching;

public record CachePolicy
{
    public TimeSpan? AbsoluteExpiration { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
    public CachePriority Priority { get; init; } = CachePriority.Normal;
    public List<string> Tags { get; init; } = new();
    public bool RefreshOnHit { get; init; } = false;
    
    // Predefined policies
    public static CachePolicy Short => new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.Low,
        RefreshOnHit = true
    };
    
    public static CachePolicy Medium => new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.Normal,
        RefreshOnHit = true
    };
    
    public static CachePolicy Long => new()
    {
        AbsoluteExpiration = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(15),
        Priority = CachePriority.High,
        RefreshOnHit = false
    };
    
    public static CachePolicy FileList => new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(3),
        Priority = CachePriority.High,
        Tags = new List<string> { "files", "ui-data" },
        RefreshOnHit = true
    };
    
    public static CachePolicy Categories => new()
    {
        AbsoluteExpiration = TimeSpan.FromHours(2),
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Priority = CachePriority.High,
        Tags = new List<string> { "categories", "metadata" },
        RefreshOnHit = false
    };
    
    public static CachePolicy Configurations => new()
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10),
        Priority = CachePriority.Normal,
        Tags = new List<string> { "configs", "settings" },
        RefreshOnHit = true
    };
}

public enum CachePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public record CacheStatistics
{
    public int TotalItems { get; init; }
    public long TotalMemoryUsage { get; init; }
    public int HitCount { get; init; }
    public int MissCount { get; init; }
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public int TotalRequests => HitCount + MissCount;
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

public enum CacheInvalidationStrategy
{
    None = 0,
    All = 1,
    FileData = 2,
    Categories = 3,
    Configurations = 4,
    UserInterface = 5,
    Metadata = 6
}