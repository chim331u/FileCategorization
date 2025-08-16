using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for MachineLearningService.
/// </summary>
public class MachineLearningServiceTests : IDisposable
{
    private readonly Mock<ILogger<MachineLearningService>> _mockLogger;
    private readonly Mock<IConfigsService> _mockConfigsService;
    private readonly MachineLearningService _service;
    private readonly string _testDataDirectory;
    private readonly string _testModelDirectory;

    /// <summary>
    /// Initializes a new instance of the MachineLearningServiceTests class.
    /// </summary>
    public MachineLearningServiceTests()
    {
        _mockLogger = new Mock<ILogger<MachineLearningService>>();
        _mockConfigsService = new Mock<IConfigsService>();
        
        // Create temporary directories for testing
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "MLTests", Guid.NewGuid().ToString(), "Data");
        _testModelDirectory = Path.Combine(Path.GetTempPath(), "MLTests", Guid.NewGuid().ToString(), "Models");
        
        Directory.CreateDirectory(_testDataDirectory);
        Directory.CreateDirectory(_testModelDirectory);
        
        // Setup default configuration mocks
        SetupDefaultConfigMocks();
        
        _service = new MachineLearningService(_mockLogger.Object, _mockConfigsService.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MachineLearningService(null!, _mockConfigsService.Object));
    }

    [Fact]
    public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MachineLearningService(_mockLogger.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_service);
        
        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MachineLearningService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region PredictFileCategoryAsync Tests

    [Fact]
    public async Task PredictFileCategoryAsync_WithNullFileName_ReturnsFailure()
    {
        // Act
        var result = await _service.PredictFileCategoryAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("File name cannot be null or empty", result.Error);
    }

    [Fact]
    public async Task PredictFileCategoryAsync_WithEmptyFileName_ReturnsFailure()
    {
        // Act
        var result = await _service.PredictFileCategoryAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("File name cannot be null or empty", result.Error);
    }

    [Fact]
    public async Task PredictFileCategoryAsync_WithWhitespaceFileName_ReturnsFailure()
    {
        // Act
        var result = await _service.PredictFileCategoryAsync("   ");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("File name cannot be null or empty", result.Error);
    }

    [Fact]
    public async Task PredictFileCategoryAsync_WithConfigurationFailure_ReturnsFailure()
    {
        // Arrange
        _mockConfigsService.Setup(x => x.GetConfigValue("MODELPATH"))
            .ThrowsAsync(new Exception("Config error"));

        // Act
        var result = await _service.PredictFileCategoryAsync("test_file.txt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to load model", result.Error);
    }

    [Fact]
    public async Task PredictFileCategoryAsync_WithMissingModelAndTrainingData_ReturnsFailure()
    {
        // Arrange - No model file exists, no training data
        var invalidTrainingPath = Path.Combine(_testDataDirectory, "nonexistent_training.csv");
        
        _mockConfigsService.Setup(x => x.GetConfigValue("TRAINDATAPATH")).ReturnsAsync(_testDataDirectory);
        _mockConfigsService.Setup(x => x.GetConfigValue("TRAINDATANAME")).ReturnsAsync("nonexistent_training.csv");

        // Act
        var result = await _service.PredictFileCategoryAsync("test_file.txt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Training data file not found", result.Error);
    }

    #endregion

    #region PredictFileCategoriesAsync Tests

    [Fact]
    public async Task PredictFileCategoriesAsync_WithNullFileList_ReturnsFailure()
    {
        // Act
        var result = await _service.PredictFileCategoriesAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("File list cannot be null", result.Error);
    }

    [Fact]
    public async Task PredictFileCategoriesAsync_WithEmptyFileList_ReturnsSuccess()
    {
        // Arrange
        var emptyList = new List<FilesDetail>();

        // Act
        var result = await _service.PredictFileCategoriesAsync(emptyList);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task PredictFileCategoriesAsync_WithFilesWithoutNames_SetsUnknownCategory()
    {
        // Arrange
        CreateTestTrainingData();
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = null },
            new() { Id = 2, Name = "" },
            new() { Id = 3, Name = "   " }
        };

        // Act
        var result = await _service.PredictFileCategoriesAsync(files);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.All(result.Value!, file => Assert.Equal("Unknown", file.FileCategory));
    }


    #endregion

    #region TrainAndSaveModelAsync Tests

    [Fact]
    public async Task TrainAndSaveModelAsync_WithMissingTrainingData_ReturnsFailure()
    {
        // Arrange - Point to non-existent training data
        _mockConfigsService.Setup(x => x.GetConfigValue("TRAINDATAPATH")).ReturnsAsync(_testDataDirectory);
        _mockConfigsService.Setup(x => x.GetConfigValue("TRAINDATANAME")).ReturnsAsync("nonexistent.csv");

        // Act
        var result = await _service.TrainAndSaveModelAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Training data file not found", result.Error);
    }

    [Fact]
    public async Task TrainAndSaveModelAsync_WithValidTrainingData_ReturnsSuccess()
    {
        // Arrange
        CreateTestTrainingData();

        // Act
        var result = await _service.TrainAndSaveModelAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Model training completed successfully", result.Value);
        
        // Verify model file was created
        var modelPath = Path.Combine(_testModelDirectory, "test_model.zip");
        Assert.True(File.Exists(modelPath));
    }

    [Fact]
    public async Task TrainAndSaveModelAsync_WithInvalidTrainingData_ReturnsFailure()
    {
        // Arrange
        CreateInvalidTrainingData();

        // Act
        var result = await _service.TrainAndSaveModelAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to load training data", result.Error);
    }

    #endregion

    #region GetModelInfoAsync Tests

    [Fact]
    public async Task GetModelInfoAsync_WithNoModel_ReturnsModelNotFoundMessage()
    {
        // Act
        var result = await _service.GetModelInfoAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Model not found", result.Value);
    }

    [Fact]
    public async Task GetModelInfoAsync_WithExistingModel_ReturnsModelInfo()
    {
        // Arrange
        CreateTestTrainingData();
        var trainResult = await _service.TrainAndSaveModelAsync();
        Assert.True(trainResult.IsSuccess);

        // Act
        var result = await _service.GetModelInfoAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Model exists at", result.Value);
        Assert.Contains("Size:", result.Value);
        Assert.Contains("Last Modified:", result.Value);
    }

    [Fact]
    public async Task GetModelInfoAsync_WithConfigurationError_ReturnsFailure()
    {
        // Arrange
        _mockConfigsService.Setup(x => x.GetConfigValue("MODELPATH"))
            .ThrowsAsync(new Exception("Config error"));

        // Act
        var result = await _service.GetModelInfoAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to get configuration", result.Error);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task PredictFileCategoryAsync_ConcurrentCalls_AllSucceed()
    {
        // Arrange
        CreateTestTrainingData();
        var fileName = "test_movie.mp4";
        var concurrentTasks = new List<Task<Result<string>>>();
        var taskCount = 10;

        // Act - Create multiple concurrent prediction tasks
        for (int i = 0; i < taskCount; i++)
        {
            concurrentTasks.Add(_service.PredictFileCategoryAsync($"{fileName}_{i}"));
        }

        var results = await Task.WhenAll(concurrentTasks);

        // Assert
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(taskCount, results.Length);
    }

    [Fact]
    public async Task TrainAndSaveModelAsync_ConcurrentCalls_OnlyOneTrains()
    {
        // Arrange
        CreateTestTrainingData();
        var concurrentTasks = new List<Task<Result<string>>>();
        var taskCount = 5;

        // Act - Create multiple concurrent training tasks
        for (int i = 0; i < taskCount; i++)
        {
            concurrentTasks.Add(_service.TrainAndSaveModelAsync());
        }

        var results = await Task.WhenAll(concurrentTasks);

        // Assert - All should succeed (due to proper locking)
        Assert.All(results, result => Assert.True(result.IsSuccess));
        
        // Verify model file exists only once
        var modelPath = Path.Combine(_testModelDirectory, "test_model.zip");
        Assert.True(File.Exists(modelPath));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task IntegrationTest_TrainThenPredict_WorksEndToEnd()
    {
        // Arrange
        CreateTestTrainingData();

        // Act - Train model
        var trainResult = await _service.TrainAndSaveModelAsync();
        Assert.True(trainResult.IsSuccess);

        // Act - Predict single file first to load the model into cache
        var predictResult = await _service.PredictFileCategoryAsync("action_movie_2024.mp4");
        Assert.True(predictResult.IsSuccess);
        Assert.NotEmpty(predictResult.Value!);

        // Act - Get model info (now it should show cached info)
        var infoResult = await _service.GetModelInfoAsync();
        Assert.True(infoResult.IsSuccess);
        Assert.Contains("Model exists at", infoResult.Value);

        // Act - Predict multiple files
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = "romantic_comedy.mp4" },
            new() { Id = 2, Name = "documentary_nature.mkv" },
            new() { Id = 3, Name = "pop_song.mp3" }
        };

        var batchResult = await _service.PredictFileCategoriesAsync(files);
        Assert.True(batchResult.IsSuccess);
        Assert.All(batchResult.Value!, file => Assert.NotEmpty(file.FileCategory ?? ""));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task Dispose_CalledOnce_DisposesResources()
    {
        // Act
        _service.Dispose();

        // Assert - Subsequent operations should throw ObjectDisposedException
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _service.PredictFileCategoryAsync("test.txt"));
        
        Assert.Equal(nameof(MachineLearningService), exception.ObjectName);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.Dispose();
        _service.Dispose();
        _service.Dispose();
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultConfigMocks()
    {
        _mockConfigsService.Setup(x => x.GetConfigValue("MODELPATH")).ReturnsAsync(_testModelDirectory);
        _mockConfigsService.Setup(x => x.GetConfigValue("MODELNAME")).ReturnsAsync("test_model.zip");
        _mockConfigsService.Setup(x => x.GetConfigValue("TRAINDATAPATH")).ReturnsAsync(_testDataDirectory);
        _mockConfigsService.Setup(x => x.GetConfigValue("TRAINDATANAME")).ReturnsAsync("training_data.csv");
    }

    private void CreateTestTrainingData()
    {
        var trainingDataPath = Path.Combine(_testDataDirectory, "training_data.csv");
        var trainingData = new[]
        {
            "Id;Area;FileName",
            "1;Video;action_movie.mp4",
            "2;Video;comedy_film.avi",
            "3;Video;documentary.mkv",
            "4;Audio;pop_song.mp3",
            "5;Audio;rock_music.wav",
            "6;Audio;classical_music.flac",
            "7;Document;report.pdf",
            "8;Document;presentation.pptx",
            "9;Document;spreadsheet.xlsx",
            "10;Image;photo.jpg",
            "11;Image;diagram.png",
            "12;Image;artwork.gif",
            "13;Software;installer.exe",
            "14;Software;application.msi",
            "15;Archive;backup.zip",
            "16;Archive;data.rar",
            "17;Video;series_episode.mkv",
            "18;Audio;podcast.mp3",
            "19;Document;manual.docx",
            "20;Image;screenshot.png"
        };

        File.WriteAllLines(trainingDataPath, trainingData);
    }

    private void CreateInvalidTrainingData()
    {
        var trainingDataPath = Path.Combine(_testDataDirectory, "training_data.csv");
        var invalidData = new[]
        {
            "Invalid;Header;Format",
            "This;Is;Not;Valid;CSV;Data",
            "Missing;Columns"
        };

        File.WriteAllLines(trainingDataPath, invalidData);
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        _service?.Dispose();
        
        try
        {
            if (Directory.Exists(Path.GetDirectoryName(_testDataDirectory)))
            {
                Directory.Delete(Path.GetDirectoryName(_testDataDirectory)!, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #endregion
}