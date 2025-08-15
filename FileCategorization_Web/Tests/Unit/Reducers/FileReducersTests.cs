using System.Collections.Immutable;
using Xunit;
using FluentAssertions;
using FileCategorization_Web.Features.FileManagement.Store;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Features.FileManagement.Reducers;
using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;
using FileCategorization_Web.Data.Caching;

namespace FileCategorization_Web.Tests.Unit.Reducers;

public class FileReducersTests
{
    private readonly FileState _initialState;

    public FileReducersTests()
    {
        _initialState = FileState.InitialState;
    }

    #region Loading State Reducers Tests

    [Fact]
    public void ReduceSetLoadingAction_SetsLoadingStateAndClearsError()
    {
        // Arrange
        var state = _initialState with { Error = "Previous error" };
        var action = new SetLoadingAction(true);

        // Act
        var result = FileReducers.ReduceSetLoadingAction(state, action);

        // Assert
        result.IsLoading.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ReduceSetLoadingAction_WhenSettingToFalse_PreservesExistingError()
    {
        // Arrange
        var state = _initialState with { Error = "Existing error", IsLoading = true };
        var action = new SetLoadingAction(false);

        // Act
        var result = FileReducers.ReduceSetLoadingAction(state, action);

        // Assert
        result.IsLoading.Should().BeFalse();
        result.Error.Should().Be("Existing error");
    }

    [Fact]
    public void ReduceSetErrorAction_SetsErrorAndClearsAllLoadingStates()
    {
        // Arrange
        var state = _initialState with 
        { 
            IsLoading = true, 
            IsRefreshing = true, 
            IsTraining = true 
        };
        var action = new SetErrorAction("Test error");

        // Act
        var result = FileReducers.ReduceSetErrorAction(state, action);

        // Assert
        result.Error.Should().Be("Test error");
        result.IsLoading.Should().BeFalse();
        result.IsRefreshing.Should().BeFalse();
        result.IsTraining.Should().BeFalse();
    }

    #endregion

    #region File Data Reducers Tests

    [Fact]
    public void ReduceLoadFilesAction_SetsLoadingAndSearchParameter()
    {
        // Arrange
        var action = new LoadFilesAction(5);

        // Act
        var result = FileReducers.ReduceLoadFilesAction(_initialState, action);

        // Assert
        result.IsLoading.Should().BeTrue();
        result.Error.Should().BeNull();
        result.SearchParameter.Should().Be(5);
    }

    [Fact]
    public void ReduceLoadFilesSuccessAction_SetsFilesAndClearsLoading()
    {
        // Arrange
        var files = ImmutableList.Create(
            new FilesDetailDto { Id = 1, Name = "File1.txt" },
            new FilesDetailDto { Id = 2, Name = "File2.txt" }
        );
        var action = new LoadFilesSuccessAction(files);
        var state = _initialState with { IsLoading = true };

        // Act
        var result = FileReducers.ReduceLoadFilesSuccessAction(state, action);

        // Assert
        result.Files.Should().HaveCount(2);
        result.Files.Should().BeEquivalentTo(files);
        result.IsLoading.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ReduceLoadFilesFailureAction_SetsErrorAndClearsLoading()
    {
        // Arrange
        var action = new LoadFilesFailureAction("Load failed");
        var state = _initialState with { IsLoading = true };

        // Act
        var result = FileReducers.ReduceLoadFilesFailureAction(state, action);

        // Assert
        result.IsLoading.Should().BeFalse();
        result.Error.Should().Be("Load failed");
    }

    #endregion

    #region Category Reducers Tests

    [Fact]
    public void ReduceLoadCategoriesSuccessAction_SetsCategories()
    {
        // Arrange
        var categories = ImmutableList.Create("Category1", "Category2", "Category3");
        var action = new LoadCategoriesSuccessAction(categories);

        // Act
        var result = FileReducers.ReduceLoadCategoriesSuccessAction(_initialState, action);

        // Assert
        result.Categories.Should().HaveCount(3);
        result.Categories.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public void ReduceAddNewCategoryAction_AddsNewCategory()
    {
        // Arrange
        var state = _initialState with 
        { 
            Categories = ImmutableList.Create("Existing1", "Existing2") 
        };
        var action = new AddNewCategoryAction("NewCategory");

        // Act
        var result = FileReducers.ReduceAddNewCategoryAction(state, action);

        // Assert
        result.Categories.Should().HaveCount(3);
        result.Categories.Should().Contain("NewCategory");
    }

    [Fact]
    public void ReduceAddNewCategoryAction_DoesNotAddDuplicateCategory()
    {
        // Arrange
        var state = _initialState with 
        { 
            Categories = ImmutableList.Create("Existing1", "Existing2") 
        };
        var action = new AddNewCategoryAction("Existing1");

        // Act
        var result = FileReducers.ReduceAddNewCategoryAction(state, action);

        // Assert
        result.Categories.Should().HaveCount(2);
        result.Categories.Should().BeEquivalentTo(state.Categories);
    }

    #endregion

    #region File Management Reducers Tests

    [Fact]
    public void ReduceUpdateFileDetailSuccessAction_UpdatesExistingFile()
    {
        // Arrange
        var originalFile = new FilesDetailDto { Id = 1, Name = "Original.txt", FileCategory = "Old" };
        var updatedFile = new FilesDetailDto { Id = 1, Name = "Updated.txt", FileCategory = "New" };
        
        var state = _initialState with 
        { 
            Files = ImmutableList.Create(originalFile, new FilesDetailDto { Id = 2, Name = "Other.txt" }) 
        };
        var action = new UpdateFileDetailSuccessAction(updatedFile);

        // Act
        var result = FileReducers.ReduceUpdateFileDetailSuccessAction(state, action);

        // Assert
        result.Files.Should().HaveCount(2);
        result.Files.Should().Contain(f => f.Id == 1 && f.Name == "Updated.txt" && f.FileCategory == "New");
        result.Files.Should().Contain(f => f.Id == 2); // Other file unchanged
    }

    [Fact]
    public void ReduceScheduleFileAction_SetsIsNotToMoveToTrue()
    {
        // Arrange
        var file = new FilesDetailDto { Id = 1, Name = "Test.txt", IsNotToMove = false };
        var state = _initialState with { Files = ImmutableList.Create(file) };
        var action = new ScheduleFileAction(1);

        // Act
        var result = FileReducers.ReduceScheduleFileAction(state, action);

        // Assert
        var updatedFile = result.Files.Single(f => f.Id == 1);
        updatedFile.IsNotToMove.Should().BeTrue();
    }

    [Fact]
    public void ReduceRevertFileAction_SetsIsNotToMoveToFalse()
    {
        // Arrange
        var file = new FilesDetailDto { Id = 1, Name = "Test.txt", IsNotToMove = true };
        var state = _initialState with { Files = ImmutableList.Create(file) };
        var action = new RevertFileAction(1);

        // Act
        var result = FileReducers.ReduceRevertFileAction(state, action);

        // Assert
        var updatedFile = result.Files.Single(f => f.Id == 1);
        updatedFile.IsNotToMove.Should().BeFalse();
    }

    #endregion

    #region Console Reducers Tests

    [Fact]
    public void ReduceAddConsoleMessageAction_AddsMessageWithTimestamp()
    {
        // Arrange
        var action = new AddConsoleMessageAction("Test message");

        // Act
        var result = FileReducers.ReduceAddConsoleMessageAction(_initialState, action);

        // Assert
        result.ConsoleMessages.Should().HaveCount(1);
        result.ConsoleMessages.Single().Should().Contain("Test message");
        result.ConsoleMessages.Single().Should().Contain(DateTime.Now.ToString("G")[..10]); // Date part
    }

    [Fact]
    public void ReduceClearConsoleAction_ClearsAllMessages()
    {
        // Arrange
        var state = _initialState with 
        { 
            ConsoleMessages = ImmutableList.Create("Message 1", "Message 2") 
        };
        var action = new ClearConsoleAction();

        // Act
        var result = FileReducers.ReduceClearConsoleAction(state, action);

        // Assert
        result.ConsoleMessages.Should().BeEmpty();
    }

    #endregion

    #region Cache Reducers Tests

    [Fact]
    public void ReduceCacheWarmupAction_SetsWarmingStateAndAddsMessage()
    {
        // Arrange
        var action = new CacheWarmupAction();

        // Act
        var result = FileReducers.ReduceCacheWarmupAction(_initialState, action);

        // Assert
        result.IsCacheWarming.Should().BeTrue();
        result.ConsoleMessages.Should().HaveCount(1);
        result.ConsoleMessages.Single().Should().Contain("Starting cache warmup");
    }

    [Fact]
    public void ReduceCacheWarmupSuccessAction_ClearsWarmingStateAndUpdatesTimestamp()
    {
        // Arrange
        var state = _initialState with { IsCacheWarming = true };
        var action = new CacheWarmupSuccessAction();

        // Act
        var result = FileReducers.ReduceCacheWarmupSuccessAction(state, action);

        // Assert
        result.IsCacheWarming.Should().BeFalse();
        result.LastCacheUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ConsoleMessages.Should().HaveCount(1);
        result.ConsoleMessages.Single().Should().Contain("Cache warmup completed");
    }

    [Fact]
    public void ReduceCacheStatsUpdateAction_UpdatesCacheStatistics()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalItems = 10,
            HitCount = 15,
            MissCount = 5,
            TotalMemoryUsage = 1024,
            LastUpdated = DateTime.UtcNow
        };
        var action = new CacheStatsUpdateAction(stats);

        // Act
        var result = FileReducers.ReduceCacheStatsUpdateAction(_initialState, action);

        // Assert
        result.CacheStatistics.Should().BeEquivalentTo(stats);
        result.LastCacheUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ReduceCacheHitAction_AddsHitMessageToConsole()
    {
        // Arrange
        var action = new CacheHitAction("test-key", "string");

        // Act
        var result = FileReducers.ReduceCacheHitAction(_initialState, action);

        // Assert
        result.ConsoleMessages.Should().HaveCount(1);
        var message = result.ConsoleMessages.Single();
        message.Should().Contain("Cache HIT");
        message.Should().Contain("test-key");
        message.Should().Contain("string");
    }

    [Fact]
    public void ReduceCacheMissAction_AddsMissMessageToConsole()
    {
        // Arrange
        var action = new CacheMissAction("test-key", "string");

        // Act
        var result = FileReducers.ReduceCacheMissAction(_initialState, action);

        // Assert
        result.ConsoleMessages.Should().HaveCount(1);
        var message = result.ConsoleMessages.Single();
        message.Should().Contain("Cache MISS");
        message.Should().Contain("test-key");
        message.Should().Contain("string");
    }

    #endregion

    #region SignalR Reducers Tests

    [Fact]
    public void ReduceSignalRConnectedAction_AddsConnectionMessage()
    {
        // Arrange
        var action = new SignalRConnectedAction("conn-123");

        // Act
        var result = FileReducers.ReduceSignalRConnectedAction(_initialState, action);

        // Assert
        result.ConsoleMessages.Should().HaveCount(1);
        var message = result.ConsoleMessages.Single();
        message.Should().Contain("SignalR - Connection established");
        message.Should().Contain("conn-123");
    }

    [Fact]
    public void ReduceSignalRFileMovedAction_WithCompletedResult_RemovesFileFromList()
    {
        // Arrange
        var file1 = new FilesDetailDto { Id = 1, Name = "File1.txt" };
        var file2 = new FilesDetailDto { Id = 2, Name = "File2.txt" };
        var state = _initialState with { Files = ImmutableList.Create(file1, file2) };
        var action = new SignalRFileMovedAction(1, "File moved successfully", MoveFilesResults.Completed);

        // Act
        var result = FileReducers.ReduceSignalRFileMovedAction(state, action);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files.Should().NotContain(f => f.Id == 1);
        result.Files.Should().Contain(f => f.Id == 2);
        result.ConsoleMessages.Should().HaveCount(1);
        result.ConsoleMessages.Single().Should().Contain("File moved successfully: Completed");
    }

    [Fact]
    public void ReduceSignalRFileMovedAction_WithNonCompletedResult_KeepsFileInList()
    {
        // Arrange
        var file1 = new FilesDetailDto { Id = 1, Name = "File1.txt" };
        var state = _initialState with { Files = ImmutableList.Create(file1) };
        var action = new SignalRFileMovedAction(1, "File move failed", MoveFilesResults.Failed);

        // Act
        var result = FileReducers.ReduceSignalRFileMovedAction(state, action);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files.Should().Contain(f => f.Id == 1);
        result.ConsoleMessages.Should().HaveCount(1);
        result.ConsoleMessages.Single().Should().Contain("File move failed: Failed");
    }

    #endregion
}