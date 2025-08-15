using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Fluxor;
using FileCategorization_Web.Features.FileManagement.Effects;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Services.Caching;
using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;
using FileCategorization_Web.Data.Common;
using FileCategorization_Web.Data.Caching;
using FileCategorization_Web.Tests.Helpers;

namespace FileCategorization_Web.Tests.Unit.Effects;

public class FileEffectsTests
{
    private readonly Mock<IFileCategorizationService> _fileServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<FileEffects>> _loggerMock;
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly FileEffects _fileEffects;

    public FileEffectsTests()
    {
        _fileServiceMock = MockServiceHelper.CreateMockFileCategorizationService();
        _cacheServiceMock = MockServiceHelper.CreateMockCacheService();
        _loggerMock = new Mock<ILogger<FileEffects>>();
        _dispatcherMock = new Mock<IDispatcher>();

        _fileEffects = new FileEffects(
            _fileServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    #region LoadFiles Tests

    [Fact]
    public async Task HandleLoadFilesAction_WithCacheHit_ReturnsFromCache()
    {
        // Arrange
        var action = new LoadFilesAction(3);
        var cachedFiles = new List<FilesDetailDto>
        {
            new() { Id = 1, Name = "CachedFile.txt" }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<IEnumerable<FilesDetailDto>>("files_3"))
            .ReturnsAsync(cachedFiles);

        // Act
        await _fileEffects.HandleLoadFilesAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheHitAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadFilesSuccessAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<AddConsoleMessageAction>()), Times.Once);

        // Should not call API service when cache hit
        _fileServiceMock.Verify(x => x.GetFileListAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HandleLoadFilesAction_WithCacheMiss_CallsApiAndCaches()
    {
        // Arrange
        var action = new LoadFilesAction(3);
        var apiFiles = new List<FilesDetailDto>
        {
            new() { Id = 1, Name = "ApiFile.txt" }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<IEnumerable<FilesDetailDto>>("files_3"))
            .ReturnsAsync((IEnumerable<FilesDetailDto>?)null);

        _fileServiceMock.Setup(x => x.GetFileListAsync(3))
            .ReturnsAsync(Result<List<FilesDetailDto>>.Success(apiFiles));

        // Act
        await _fileEffects.HandleLoadFilesAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheMissAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadFilesSuccessAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheSetAction>()), Times.Once);

        _fileServiceMock.Verify(x => x.GetFileListAsync(3), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync("files_3", apiFiles, CachePolicy.FileList), Times.Once);
    }

    [Fact]
    public async Task HandleLoadFilesAction_WithApiFailure_DispatchesFailureAction()
    {
        // Arrange
        var action = new LoadFilesAction(3);
        _cacheServiceMock.Setup(x => x.GetAsync<IEnumerable<FilesDetailDto>>("files_3"))
            .ReturnsAsync((IEnumerable<FilesDetailDto>?)null);

        _fileServiceMock.Setup(x => x.GetFileListAsync(3))
            .ReturnsAsync(Result<List<FilesDetailDto>>.Failure("API Error"));

        // Act
        await _fileEffects.HandleLoadFilesAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<LoadFilesFailureAction>(a => a.Error == "API Error")), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CachePolicy>()), Times.Never);
    }

    [Fact]
    public async Task HandleLoadFilesAction_WithException_DispatchesFailureAction()
    {
        // Arrange
        var action = new LoadFilesAction(3);
        _cacheServiceMock.Setup(x => x.GetAsync<IEnumerable<FilesDetailDto>>("files_3"))
            .ThrowsAsync(new Exception("Cache exception"));

        // Act
        await _fileEffects.HandleLoadFilesAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<LoadFilesFailureAction>(
            a => a.Error.Contains("Error loading files: Cache exception"))), Times.Once);
    }

    #endregion

    #region LoadCategories Tests

    [Fact]
    public async Task HandleLoadCategoriesAction_WithCacheHit_ReturnsFromCache()
    {
        // Arrange
        var action = new LoadCategoriesAction();
        var cachedCategories = new List<string> { "Category1", "Category2" };

        _cacheServiceMock.Setup(x => x.GetAsync<IEnumerable<string>>("categories"))
            .ReturnsAsync(cachedCategories);

        // Act
        await _fileEffects.HandleLoadCategoriesAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheHitAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadCategoriesSuccessAction>()), Times.Once);

        _fileServiceMock.Verify(x => x.GetCategoryListAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleLoadCategoriesAction_WithCacheMiss_CallsApiAndCaches()
    {
        // Arrange
        var action = new LoadCategoriesAction();
        var apiCategories = new List<string> { "ApiCategory1", "ApiCategory2" };

        _cacheServiceMock.Setup(x => x.GetAsync<IEnumerable<string>>("categories"))
            .ReturnsAsync((IEnumerable<string>?)null);

        _fileServiceMock.Setup(x => x.GetCategoryListAsync())
            .ReturnsAsync(Result<List<string>>.Success(apiCategories));

        // Act
        await _fileEffects.HandleLoadCategoriesAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheMissAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadCategoriesSuccessAction>()), Times.Once);

        _fileServiceMock.Verify(x => x.GetCategoryListAsync(), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync("categories", apiCategories, CachePolicy.Categories), Times.Once);
    }

    #endregion

    #region UpdateFileDetail Tests

    [Fact]
    public async Task HandleUpdateFileDetailAction_Success_InvalidatesCacheAndDispatchesSuccess()
    {
        // Arrange
        var file = new FilesDetailDto { Id = 1, Name = "Test.txt" };
        var updatedFile = new FilesDetailDto { Id = 1, Name = "Updated.txt" };
        var action = new UpdateFileDetailAction(file);

        _fileServiceMock.Setup(x => x.UpdateFileDetailAsync(file))
            .ReturnsAsync(Result<FilesDetailDto>.Success(updatedFile));

        // Act
        await _fileEffects.HandleUpdateFileDetailAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync("files"), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<UpdateFileDetailSuccessAction>(a => a.UpdatedFile == updatedFile)), Times.Once);
    }

    [Fact]
    public async Task HandleUpdateFileDetailAction_Failure_DispatchesFailureAction()
    {
        // Arrange
        var file = new FilesDetailDto { Id = 1, Name = "Test.txt" };
        var action = new UpdateFileDetailAction(file);

        _fileServiceMock.Setup(x => x.UpdateFileDetailAsync(file))
            .ReturnsAsync(Result<FilesDetailDto>.Failure("Update failed"));

        // Act
        await _fileEffects.HandleUpdateFileDetailAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync("files"), Times.Never);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<UpdateFileDetailFailureAction>()), Times.Once);
    }

    #endregion

    #region RefreshData Tests

    [Fact]
    public async Task HandleRefreshDataAction_Success_InvalidatesCacheAndDispatchesLoadCategories()
    {
        // Arrange
        var action = new RefreshDataAction();
        _fileServiceMock.Setup(x => x.RefreshCategoryAsync())
            .ReturnsAsync(Result<string>.Success("Refreshed successfully"));

        // Act
        await _fileEffects.HandleRefreshDataAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync("categories"), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync("files"), Times.Once);
        
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<RefreshDataSuccessAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadCategoriesAction>()), Times.Once);
    }

    #endregion

    #region Cache Management Effects Tests

    [Fact]
    public async Task HandleCacheClearAction_Success_DispatchesSuccessAction()
    {
        // Arrange
        var action = new CacheClearAction();
        _cacheServiceMock.Setup(x => x.ClearAllAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _fileEffects.HandleCacheClearAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.ClearAllAsync(), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheClearSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleCacheClearAction_Exception_DispatchesFailureAction()
    {
        // Arrange
        var action = new CacheClearAction();
        _cacheServiceMock.Setup(x => x.ClearAllAsync())
            .ThrowsAsync(new Exception("Clear failed"));

        // Act
        await _fileEffects.HandleCacheClearAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<CacheClearFailureAction>(
            a => a.Error.Contains("Failed to clear cache: Clear failed"))), Times.Once);
    }

    [Theory]
    [InlineData(CacheInvalidationStrategy.FileData, "files")]
    [InlineData(CacheInvalidationStrategy.Categories, "categories")]
    [InlineData(CacheInvalidationStrategy.Configurations, "configurations")]
    public async Task HandleCacheInvalidateAction_WithSpecificStrategy_CallsCorrectInvalidation(
        CacheInvalidationStrategy strategy, string expectedTag)
    {
        // Arrange
        var action = new CacheInvalidateAction(strategy);

        // Act
        await _fileEffects.HandleCacheInvalidateAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync(expectedTag), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheInvalidateSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleCacheInvalidateAction_WithAllStrategy_ClearAllCache()
    {
        // Arrange
        var action = new CacheInvalidateAction(CacheInvalidationStrategy.All);

        // Act
        await _fileEffects.HandleCacheInvalidateAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.ClearAllAsync(), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheInvalidateSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleCacheWarmupAction_Success_DispatchesMultipleLoadActions()
    {
        // Arrange
        var action = new CacheWarmupAction();
        var stats = new CacheStatistics { TotalItems = 5 };
        _cacheServiceMock.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(stats);

        // Act
        await _fileEffects.HandleCacheWarmupAction(action, _dispatcherMock.Object);

        // Assert
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadCategoriesAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadConfigurationsAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<LoadFilesAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<CacheStatsUpdateAction>(a => a.Statistics == stats)), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<CacheWarmupSuccessAction>()), Times.Once);
    }

    #endregion

    #region Move Files Tests

    [Fact]
    public async Task HandleMoveFilesAction_Success_InvalidatesCacheAndReloadsFiles()
    {
        // Arrange
        var filesToMove = ImmutableList.Create(new FilesDetailDto { Id = 1, Name = "Test.txt" });
        var action = new MoveFilesAction(filesToMove);

        _fileServiceMock.Setup(x => x.MoveFilesAsync(It.IsAny<List<FilesDetailDto>>()))
            .ReturnsAsync(Result<string>.Success("Files moved successfully"));

        // Act
        await _fileEffects.HandleMoveFilesAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync("files"), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<MoveFilesSuccessAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<LoadFilesAction>(a => a.SearchParameter == 3)), Times.Once);
    }

    #endregion

    #region Force Category Tests

    [Fact]
    public async Task HandleForceCategoryAction_Success_InvalidatesCacheAndReloadsFiles()
    {
        // Arrange
        var action = new ForceCategoryAction();
        _fileServiceMock.Setup(x => x.ForceCategoryAsync())
            .ReturnsAsync(Result<string>.Success("Categorization completed"));

        // Act
        await _fileEffects.HandleForceCategoryAction(action, _dispatcherMock.Object);

        // Assert
        _cacheServiceMock.Verify(x => x.InvalidateByTagAsync("files"), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.IsAny<ForceCategorySuccessAction>()), Times.Once);
        _dispatcherMock.Verify(x => x.Dispatch(It.Is<LoadFilesAction>(a => a.SearchParameter == 3)), Times.Once);
    }

    #endregion
}