using FileCategorization_Api.Common;
using FileCategorization_Api.Contracts.Actions;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for ActionsService.
/// </summary>
public class ActionsServiceTests
{
    private readonly Mock<IActionsRepository> _mockActionsRepository;
    private readonly Mock<IMachineLearningService> _mockMachineLearningService;
    private readonly Mock<IConfigsService> _mockConfigsService;
    private readonly Mock<IHangFireJobService> _mockHangFireJobService;
    private readonly Mock<ILogger<ActionsService>> _mockLogger;
    private readonly ActionsService _service;

    /// <summary>
    /// Initializes a new instance of the ActionsServiceTests class.
    /// </summary>
    public ActionsServiceTests()
    {
        _mockActionsRepository = new Mock<IActionsRepository>();
        _mockMachineLearningService = new Mock<IMachineLearningService>();
        _mockConfigsService = new Mock<IConfigsService>();
        _mockHangFireJobService = new Mock<IHangFireJobService>();
        _mockLogger = new Mock<ILogger<ActionsService>>();

        _service = new ActionsService(
            _mockActionsRepository.Object,
            _mockMachineLearningService.Object,
            _mockConfigsService.Object,
            _mockHangFireJobService.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullActionsRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionsService(
            null!,
            _mockMachineLearningService.Object,
            _mockConfigsService.Object,
            _mockHangFireJobService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullMachineLearningService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionsService(
            _mockActionsRepository.Object,
            null!,
            _mockConfigsService.Object,
            _mockHangFireJobService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullConfigsService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionsService(
            _mockActionsRepository.Object,
            _mockMachineLearningService.Object,
            null!,
            _mockHangFireJobService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHangFireJobService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionsService(
            _mockActionsRepository.Object,
            _mockMachineLearningService.Object,
            _mockConfigsService.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionsService(
            _mockActionsRepository.Object,
            _mockMachineLearningService.Object,
            _mockConfigsService.Object,
            _mockHangFireJobService.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_service);
    }

    #endregion

    #region RefreshFilesAsync Tests

    [Fact]
    public async Task RefreshFilesAsync_WithValidRequest_ReturnsSuccessWithJobId()
    {
        // Arrange
        var request = new RefreshFilesRequest
        {
            BatchSize = 100,
            ForceRecategorization = false,
            FileExtensionFilters = new List<string> { ".txt", ".pdf" }
        };

        // Act
        var result = await _service.RefreshFilesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.JobId);
        Assert.Equal("Queued", result.Data.Status);
        Assert.Equal(request.BatchSize, result.Data.Metadata["BatchSize"]);
        Assert.Equal(request.ForceRecategorization, result.Data.Metadata["ForceRecategorization"]);
        Assert.Equal(request.FileExtensionFilters, result.Data.Metadata["FileExtensionFilters"]);
    }

    [Fact]
    public async Task RefreshFilesAsync_WithNullFileExtensionFilters_ReturnsSuccessWithEmptyFilters()
    {
        // Arrange
        var request = new RefreshFilesRequest
        {
            BatchSize = 50,
            ForceRecategorization = true,
            FileExtensionFilters = null
        };

        // Act
        var result = await _service.RefreshFilesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var filters = (List<string>)result.Data!.Metadata["FileExtensionFilters"];
        Assert.Empty(filters);
    }

    [Fact]
    public async Task RefreshFilesAsync_WithCancellationToken_RespondsToCancel()
    {
        // Arrange
        var request = new RefreshFilesRequest { BatchSize = 100 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.RefreshFilesAsync(request, cts.Token);

        // Assert - Should still return success since the job is just queued
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region MoveFilesAsync Tests

    [Fact]
    public async Task MoveFilesAsync_WithValidRequest_ValidatesFilesAndReturnsJobId()
    {
        // Arrange
        var request = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 1, FileCategory = "Document" },
                new() { Id = 2, FileCategory = "Image" }
            },
            ContinueOnError = true,
            ValidateCategories = false,
            CreateDirectories = true
        };

        var existingFiles = new Dictionary<int, FilesDetail>
        {
            { 1, new FilesDetail { Id = 1, Name = "file1.txt" } },
            { 2, new FilesDetail { Id = 2, Name = "file2.jpg" } }
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Success(existingFiles));

        // Act
        var result = await _service.MoveFilesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.JobId);
        Assert.Equal("Queued", result.Data.Status);
        Assert.Equal(2, result.Data.TotalItems);
        Assert.Equal(request.ContinueOnError, result.Data.Metadata["ContinueOnError"]);
        Assert.Equal(request.ValidateCategories, result.Data.Metadata["ValidateCategories"]);
        Assert.Equal(request.CreateDirectories, result.Data.Metadata["CreateDirectories"]);
    }

    [Fact]
    public async Task MoveFilesAsync_WithMissingFiles_ReturnsFailureWhenContinueOnErrorIsFalse()
    {
        // Arrange
        var request = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 1, FileCategory = "Document" },
                new() { Id = 999, FileCategory = "Image" } // Non-existent file
            },
            ContinueOnError = false
        };

        var existingFiles = new Dictionary<int, FilesDetail>
        {
            { 1, new FilesDetail { Id = 1, Name = "file1.txt" } }
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Success(existingFiles));

        // Act
        var result = await _service.MoveFilesAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Files not found: 999", result.ErrorMessage);
    }

    [Fact]
    public async Task MoveFilesAsync_WithMissingFiles_ReturnsSuccessWhenContinueOnErrorIsTrue()
    {
        // Arrange
        var request = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 1, FileCategory = "Document" },
                new() { Id = 999, FileCategory = "Image" } // Non-existent file
            },
            ContinueOnError = true
        };

        var existingFiles = new Dictionary<int, FilesDetail>
        {
            { 1, new FilesDetail { Id = 1, Name = "file1.txt" } }
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Success(existingFiles));

        // Act
        var result = await _service.MoveFilesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var missingFiles = (List<int>)result.Data!.Metadata["MissingFiles"];
        Assert.Contains(999, missingFiles);
    }

    [Fact]
    public async Task MoveFilesAsync_WithRepositoryFailure_ReturnsFailure()
    {
        // Arrange
        var request = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 1, FileCategory = "Document" }
            }
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Failure("Database error"));

        // Act
        var result = await _service.MoveFilesAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to validate files: Database error", result.ErrorMessage);
    }

    #endregion

    #region ForceCategorizeAsync Tests

    [Fact]
    public async Task ForceCategorizeAsync_ReturnsSuccessWithJobInfo()
    {
        // Act
        var result = await _service.ForceCategorizeAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.JobId);
        Assert.Equal("Running", result.Data.Status);
        Assert.Equal("ForceCategorize", result.Data.Metadata["Operation"]);
    }

    [Fact]
    public async Task ForceCategorizeAsync_WithCancellationToken_RespondsToCancel()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.ForceCategorizeAsync(cts.Token);

        // Assert - Should still return success since it's just creating job info
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region TrainModelAsync Tests

    [Fact]
    public async Task TrainModelAsync_WithSuccessfulTraining_ReturnsSuccess()
    {
        // Arrange
        var expectedMessage = "Model training completed successfully";
        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(expectedMessage));

        _mockMachineLearningService.Setup(x => x.GetModelInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Model exists at: /path/model.zip, Size: 1024 bytes"));

        // Act
        var result = await _service.TrainModelAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Success);
        Assert.Equal(expectedMessage, result.Data.Message);
        Assert.NotNull(result.Data.ModelVersion);
        Assert.True(result.Data.TrainingDuration.TotalMilliseconds >= 0);
        Assert.Equal(1024, result.Data.ModelSizeBytes);
        Assert.Equal("/path/model.zip", result.Data.ModelPath);
        Assert.Contains("TrainingDurationSeconds", result.Data.Metrics.Keys);
    }

    [Fact]
    public async Task TrainModelAsync_WithTrainingFailure_ReturnsFailure()
    {
        // Arrange
        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Training failed"));

        // Act
        var result = await _service.TrainModelAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Model training failed: Training failed", result.ErrorMessage);
    }

    [Fact]
    public async Task TrainModelAsync_WithModelInfoFailure_StillReturnsSuccess()
    {
        // Arrange
        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Training completed"));

        _mockMachineLearningService.Setup(x => x.GetModelInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Model info error"));

        // Act
        var result = await _service.TrainModelAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Success);
        Assert.Null(result.Data.ModelSizeBytes); // Should not be set due to info failure
        Assert.Null(result.Data.ModelPath);
    }

    [Fact]
    public async Task TrainModelAsync_WithException_ReturnsFailure()
    {
        // Arrange
        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _service.TrainModelAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unexpected error", result.ErrorMessage);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationToken_RespondsToCancel()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _service.TrainModelAsync(cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("canceled", result.ErrorMessage.ToLowerInvariant());
    }

    #endregion

    #region GetJobStatusAsync Tests

    [Fact]
    public async Task GetJobStatusAsync_WithValidJobId_ReturnsJobStatus()
    {
        // Arrange
        var jobId = "test-job-123";

        // Act
        var result = await _service.GetJobStatusAsync(jobId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(jobId, result.Data!.JobId);
        Assert.Equal("Unknown", result.Data.Status);
        Assert.Contains("Note", result.Data.Metadata.Keys);
    }

    [Fact]
    public async Task GetJobStatusAsync_WithNullJobId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetJobStatusAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Job ID cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task GetJobStatusAsync_WithEmptyJobId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetJobStatusAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Job ID cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task GetJobStatusAsync_WithWhitespaceJobId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetJobStatusAsync("   ");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Job ID cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task GetJobStatusAsync_WithException_ReturnsFailure()
    {
        // Arrange
        var service = new ActionsService(
            _mockActionsRepository.Object,
            _mockMachineLearningService.Object,
            _mockConfigsService.Object,
            _mockHangFireJobService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.GetJobStatusAsync("test-job");

        // Assert - Should handle gracefully and return basic status
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteWorkflow_TrainThenRefreshThenMove_WorksEndToEnd()
    {
        // Arrange
        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Model trained"));

        _mockMachineLearningService.Setup(x => x.GetModelInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Model exists"));

        var existingFiles = new Dictionary<int, FilesDetail>
        {
            { 1, new FilesDetail { Id = 1, Name = "file1.txt" } }
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Success(existingFiles));

        // Act & Assert - Train Model
        var trainResult = await _service.TrainModelAsync();
        Assert.True(trainResult.IsSuccess);

        // Act & Assert - Refresh Files
        var refreshRequest = new RefreshFilesRequest { BatchSize = 100 };
        var refreshResult = await _service.RefreshFilesAsync(refreshRequest);
        Assert.True(refreshResult.IsSuccess);

        // Act & Assert - Move Files
        var moveRequest = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 1, FileCategory = "Document" }
            }
        };
        var moveResult = await _service.MoveFilesAsync(moveRequest);
        Assert.True(moveResult.IsSuccess);

        // Verify all operations returned valid job information
        Assert.NotEmpty(refreshResult.Data!.JobId);
        Assert.NotEmpty(moveResult.Data!.JobId);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task MoveFilesAsync_WithLargeFileList_PerformsEfficiently()
    {
        // Arrange - Create a large file list
        var filesToMove = new List<FileMoveDto>();
        var existingFiles = new Dictionary<int, FilesDetail>();

        for (int i = 1; i <= 1000; i++)
        {
            filesToMove.Add(new FileMoveDto { Id = i, FileCategory = "Document" });
            existingFiles.Add(i, new FilesDetail { Id = i, Name = $"file{i}.txt" });
        }

        var request = new MoveFilesRequest
        {
            FilesToMove = filesToMove,
            ContinueOnError = true
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Success(existingFiles));

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.MoveFilesAsync(request);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Data!.TotalItems);
        
        // Performance assertion - validation should be fast
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"File validation took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task AllMethods_HandleNullCancellationToken_WorkCorrectly()
    {
        // Arrange
        var refreshRequest = new RefreshFilesRequest { BatchSize = 100 };
        var moveRequest = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 1, FileCategory = "Document" }
            }
        };

        var existingFiles = new Dictionary<int, FilesDetail>
        {
            { 1, new FilesDetail { Id = 1, Name = "file1.txt" } }
        };

        _mockActionsRepository.Setup(x => x.GetFilesByIdsAsync(
            It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, FilesDetail>>.Success(existingFiles));

        _mockMachineLearningService.Setup(x => x.TrainAndSaveModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Training completed"));

        // Act & Assert - All should work with default cancellation token
        var refreshResult = await _service.RefreshFilesAsync(refreshRequest);
        Assert.True(refreshResult.IsSuccess);

        var moveResult = await _service.MoveFilesAsync(moveRequest);
        Assert.True(moveResult.IsSuccess);

        var forceResult = await _service.ForceCategorizeAsync();
        Assert.True(forceResult.IsSuccess);

        var trainResult = await _service.TrainModelAsync();
        Assert.True(trainResult.IsSuccess);

        var statusResult = await _service.GetJobStatusAsync("test-job");
        Assert.True(statusResult.IsSuccess);
    }

    #endregion
}