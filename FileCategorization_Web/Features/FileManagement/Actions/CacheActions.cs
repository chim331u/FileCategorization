using FileCategorization_Web.Data.Caching;
using FileCategorization_Web.Features.FileManagement.Actions;

namespace FileCategorization_Web.Features.FileManagement.Actions;

// Cache Management Actions
public record CacheClearAction : FileAction;
public record CacheClearSuccessAction : FileAction;
public record CacheClearFailureAction(string Error) : FileAction;

public record CacheInvalidateAction(CacheInvalidationStrategy Strategy) : FileAction;
public record CacheInvalidateSuccessAction(CacheInvalidationStrategy Strategy) : FileAction;
public record CacheInvalidateFailureAction(string Error) : FileAction;

public record CacheWarmupAction : FileAction;
public record CacheWarmupSuccessAction : FileAction;
public record CacheWarmupFailureAction(string Error) : FileAction;

public record CacheStatsUpdateAction(CacheStatistics Statistics) : FileAction;

// Cache Hit/Miss Actions (for monitoring)
public record CacheHitAction(string Key, string DataType) : FileAction;
public record CacheMissAction(string Key, string DataType) : FileAction;
public record CacheSetAction(string Key, string DataType, TimeSpan? ExpirationTime) : FileAction;