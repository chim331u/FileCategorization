using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Services.Caching;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using FileCategorization_Shared.Common;

namespace FileCategorization_Web.Tests.Helpers;

public static class MockServiceHelper
{
    /// <summary>
    /// Creates a mock IFileCategorizationService with common test behaviors
    /// </summary>
    public static Mock<IFileCategorizationService> CreateMockFileCategorizationService()
    {
        var mock = new Mock<IFileCategorizationService>();
        
        // Default successful responses
        mock.Setup(x => x.GetFileListAsync(It.IsAny<int>()))
            .ReturnsAsync(Result<List<FilesDetailDto>>.Success(new List<FilesDetailDto>()));
            
        mock.Setup(x => x.GetCategoryListAsync())
            .ReturnsAsync(Result<List<string>>.Success(new List<string> { "Category1", "Category2" }));
            
        mock.Setup(x => x.GetConfigListAsync())
            .ReturnsAsync(Result<List<ConfigsDto>>.Success(new List<ConfigsDto>()));
            
        mock.Setup(x => x.RefreshCategoryAsync())
            .ReturnsAsync(Result<string>.Success("Refresh completed"));
            
        return mock;
    }
    
    /// <summary>
    /// Creates a mock ICacheService with standard test behavior
    /// </summary>
    public static Mock<ICacheService> CreateMockCacheService()
    {
        var mock = new Mock<ICacheService>();
        
        // Default cache miss behavior (returns null)
        mock.Setup(x => x.GetAsync<object>(It.IsAny<string>()))
            .ReturnsAsync((object?)null);
            
        // Default successful cache operations
        mock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Data.Caching.CachePolicy?>()))
            .Returns(Task.CompletedTask);
            
        mock.Setup(x => x.InvalidateByTagAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
            
        return mock;
    }
    
    /// <summary>
    /// Creates a real MemoryCacheService for integration testing
    /// </summary>
    public static MemoryCacheService CreateRealMemoryCacheService(IServiceProvider? serviceProvider = null)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000
        });
        
        var logger = serviceProvider?.GetService<ILogger<MemoryCacheService>>() 
                    ?? Mock.Of<ILogger<MemoryCacheService>>();
        
        return new MemoryCacheService(memoryCache, logger);
    }
    
    /// <summary>
    /// Adds common mock services to a service collection for testing
    /// </summary>
    public static IServiceCollection AddMockServices(this IServiceCollection services)
    {
        // Add mock file categorization service
        var mockFileService = CreateMockFileCategorizationService();
        services.AddScoped(_ => mockFileService.Object);
        
        // Add mock cache service
        var mockCacheService = CreateMockCacheService();
        services.AddScoped(_ => mockCacheService.Object);
        
        return services;
    }
}