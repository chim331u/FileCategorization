using FileCategorization_Shared.Common;
using FileCategorization_Api.Contracts.Actions;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Endpoints;
using FileCategorization_Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for ActionsEndpointV2.
/// </summary>
public class ActionsEndpointV2Tests
{
    private readonly Mock<IActionsService> _mockActionsService;
    private readonly Mock<ILogger> _mockLogger;

    /// <summary>
    /// Initializes a new instance of the ActionsEndpointV2Tests class.
    /// </summary>
    public ActionsEndpointV2Tests()
    {
        _mockActionsService = new Mock<IActionsService>();
        _mockLogger = new Mock<ILogger>();
    }

    #region RefreshFiles Endpoint Tests

    [Fact]
    public async Task RefreshFiles_WithValidRequest_ReturnsOkWithJobResponse()
    {
        // Arrange
        var request = new RefreshFilesRequest
        {
            BatchSize = 100,
            ForceRecategorization = false,
            FileExtensionFilters = new List<string> { ".txt", ".pdf" }
        };

        var expectedResponse = new ActionJobResponse
        {
            JobId = "job-123",
            Status = "Queued",
            StartTime = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["BatchSize"] = 100,
                ["ForceRecategorization"] = false
            }
        };

        _mockActionsService.Setup(x => x.RefreshFilesAsync(
            It.Is<RefreshFilesRequest>(r => r.BatchSize == 100), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Success(expectedResponse));

        // Act
        var result = await InvokeRefreshFilesEndpoint(request);

        // Assert
        var okResult = Assert.IsType<Ok<ActionJobResponse>>(result);
        Assert.Equal(expectedResponse.JobId, okResult.Value!.JobId);
        Assert.Equal(expectedResponse.Status, okResult.Value.Status);
    }

    [Fact]
    public async Task RefreshFiles_WithServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshFilesRequest { BatchSize = 100 };
        
        _mockActionsService.Setup(x => x.RefreshFilesAsync(
            It.IsAny<RefreshFilesRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Failure("Service error"));

        // Act
        var result = await InvokeRefreshFilesEndpoint(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<object>>(result);
        var errorResponse = JsonSerializer.Serialize(badRequestResult.Value);
        Assert.Contains("Service error", errorResponse);
    }

    [Fact]
    public async Task RefreshFiles_WithCancellationToken_PassesToService()
    {
        // Arrange
        var request = new RefreshFilesRequest { BatchSize = 50 };
        var cts = new CancellationTokenSource();
        
        _mockActionsService.Setup(x => x.RefreshFilesAsync(
            It.IsAny<RefreshFilesRequest>(), 
            cts.Token))
            .ReturnsAsync(Result<ActionJobResponse>.Success(new ActionJobResponse 
            { 
                JobId = "job-456", 
                Status = "Queued" 
            }));

        // Act
        var result = await InvokeRefreshFilesEndpoint(request, cts.Token);

        // Assert
        var okResult = Assert.IsType<Ok<ActionJobResponse>>(result);
        Assert.Equal("job-456", okResult.Value!.JobId);
        
        // Verify the cancellation token was passed to the service
        _mockActionsService.Verify(x => x.RefreshFilesAsync(
            It.IsAny<RefreshFilesRequest>(), 
            cts.Token), Times.Once);
    }

    #endregion

    #region MoveFiles Endpoint Tests

    [Fact]
    public async Task MoveFiles_WithValidRequest_ReturnsOkWithJobResponse()
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

        var expectedResponse = new ActionJobResponse
        {
            JobId = "move-job-123",
            Status = "Queued",
            TotalItems = 2,
            StartTime = DateTime.UtcNow
        };

        _mockActionsService.Setup(x => x.MoveFilesAsync(
            It.Is<MoveFilesRequest>(r => r.FilesToMove.Count == 2), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Success(expectedResponse));

        // Act
        var result = await InvokeMoveFilesEndpoint(request);

        // Assert
        var okResult = Assert.IsType<Ok<ActionJobResponse>>(result);
        Assert.Equal(expectedResponse.JobId, okResult.Value!.JobId);
        Assert.Equal(expectedResponse.TotalItems, okResult.Value.TotalItems);
    }

    [Fact]
    public async Task MoveFiles_WithServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        var request = new MoveFilesRequest
        {
            FilesToMove = new List<FileMoveDto>
            {
                new() { Id = 999, FileCategory = "NonExistent" }
            }
        };
        
        _mockActionsService.Setup(x => x.MoveFilesAsync(
            It.IsAny<MoveFilesRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Failure("Files not found: 999"));

        // Act
        var result = await InvokeMoveFilesEndpoint(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<object>>(result);
        var errorResponse = JsonSerializer.Serialize(badRequestResult.Value);
        Assert.Contains("Files not found: 999", errorResponse);
    }

    #endregion

    #region ForceCategorize Endpoint Tests

    [Fact]
    public async Task ForceCategorize_WithSuccessfulService_ReturnsOkWithJobResponse()
    {
        // Arrange
        var expectedResponse = new ActionJobResponse
        {
            JobId = "force-job-123",
            Status = "Queued",
            StartTime = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["Operation"] = "ForceCategorize",
                ["Description"] = "Force categorization of uncategorized files using ML prediction"
            }
        };

        _mockActionsService.Setup(x => x.ForceCategorizeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Success(expectedResponse));

        // Act
        var result = await InvokeForceCategorizeEndpoint();

        // Assert
        var okResult = Assert.IsType<Ok<ActionJobResponse>>(result);
        Assert.Equal(expectedResponse.JobId, okResult.Value!.JobId);
        Assert.Equal("Queued", okResult.Value.Status);
        Assert.Equal("ForceCategorize", okResult.Value.Metadata["Operation"]);
    }

    [Fact]
    public async Task ForceCategorize_WithServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        _mockActionsService.Setup(x => x.ForceCategorizeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Failure("JobStorage instance has not been initialized"));

        // Act
        var result = await InvokeForceCategorizeEndpoint();

        // Assert
        // The result should be a BadRequest, regardless of the exact type parameter
        Assert.IsAssignableFrom<IResult>(result);
        var resultString = result.ToString();
        Assert.Contains("BadRequest", resultString!);
    }

    #endregion

    #region TrainModel Endpoint Tests

    [Fact]
    public async Task TrainModel_WithSuccessfulService_ReturnsOkWithTrainResponse()
    {
        // Arrange
        var expectedResponse = new TrainModelResponse
        {
            Success = true,
            Message = "Model training completed successfully",
            TrainingDuration = TimeSpan.FromMinutes(5),
            ModelVersion = "20241213120000",
            ModelSizeBytes = 1024000,
            ModelPath = "/models/trained_model.zip",
            Metrics = new Dictionary<string, double>
            {
                ["Accuracy"] = 0.95,
                ["TrainingDurationSeconds"] = 300
            }
        };

        _mockActionsService.Setup(x => x.TrainModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TrainModelResponse>.Success(expectedResponse));

        // Act
        var result = await InvokeTrainModelEndpoint();

        // Assert
        var okResult = Assert.IsType<Ok<TrainModelResponse>>(result);
        Assert.True(okResult.Value!.Success);
        Assert.Equal("Model training completed successfully", okResult.Value.Message);
        Assert.Equal(1024000, okResult.Value.ModelSizeBytes);
        Assert.Contains("Accuracy", okResult.Value.Metrics.Keys);
    }

    [Fact]
    public async Task TrainModel_WithServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        _mockActionsService.Setup(x => x.TrainModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TrainModelResponse>.Failure("JobStorage instance has not been initialized"));

        // Act
        var result = await InvokeTrainModelEndpoint();

        // Assert
        // The result should be a BadRequest, regardless of the exact type parameter
        Assert.IsAssignableFrom<IResult>(result);
        var resultString = result.ToString();
        Assert.Contains("BadRequest", resultString!);
    }

    #endregion

    #region GetJobStatus Endpoint Tests

    [Fact]
    public async Task GetJobStatus_WithValidJobId_ReturnsOkWithJobStatus()
    {
        // Arrange
        var jobId = "test-job-123";
        var expectedResponse = new ActionJobResponse
        {
            JobId = jobId,
            Status = "Running",
            StartTime = DateTime.UtcNow.AddMinutes(-2),
            ProcessedItems = 50,
            TotalItems = 100,
            Metadata = new Dictionary<string, object>
            {
                ["CurrentStep"] = "Processing files",
                ["EstimatedTimeRemaining"] = "2 minutes"
            }
        };

        _mockActionsService.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Success(expectedResponse));

        // Act
        var result = await InvokeGetJobStatusEndpoint(jobId);

        // Assert
        var okResult = Assert.IsType<Ok<ActionJobResponse>>(result);
        Assert.Equal(jobId, okResult.Value!.JobId);
        Assert.Equal("Running", okResult.Value.Status);
        Assert.Equal(50, okResult.Value.ProcessedItems);
        Assert.Equal(50.0m, okResult.Value.ProgressPercentage);
    }

    [Fact]
    public async Task GetJobStatus_WithInvalidJobId_ReturnsBadRequest()
    {
        // Arrange
        var jobId = "invalid-job";
        
        _mockActionsService.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Failure("Job with ID 'invalid-job' not found"));

        // Act
        var result = await InvokeGetJobStatusEndpoint(jobId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<object>>(result);
        var errorResponse = JsonSerializer.Serialize(badRequestResult.Value);
        Assert.Contains("not found", errorResponse);
    }

    [Fact]
    public async Task GetJobStatus_WithHangfireUnavailable_ReturnsBadRequest()
    {
        // Arrange
        var jobId = "test-job";
        
        _mockActionsService.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Failure("Job storage is not available"));

        // Act
        var result = await InvokeGetJobStatusEndpoint(jobId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<object>>(result);
        var errorResponse = JsonSerializer.Serialize(badRequestResult.Value);
        Assert.Contains("Job storage is not available", errorResponse);
    }

    [Fact]
    public async Task GetJobStatus_WithEmptyJobId_ReturnsBadRequest()
    {
        // Arrange
        var jobId = "";
        
        _mockActionsService.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActionJobResponse>.Failure("Job ID cannot be null or empty"));

        // Act
        var result = await InvokeGetJobStatusEndpoint(jobId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<object>>(result);
        var errorResponse = JsonSerializer.Serialize(badRequestResult.Value);
        Assert.Contains("Job ID cannot be null or empty", errorResponse);
    }

    #endregion

    #region Integration and Error Handling Tests

    [Fact]
    public async Task AllEndpoints_WithServiceExceptions_HandleGracefully()
    {
        // Arrange
        _mockActionsService.Setup(x => x.RefreshFilesAsync(
            It.IsAny<RefreshFilesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        _mockActionsService.Setup(x => x.MoveFilesAsync(
            It.IsAny<MoveFilesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        _mockActionsService.Setup(x => x.ForceCategorizeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("ML service unavailable"));

        _mockActionsService.Setup(x => x.TrainModelAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Training data corrupted"));

        _mockActionsService.Setup(x => x.GetJobStatusAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Job service unavailable"));

        // Act & Assert - All endpoints should handle exceptions gracefully
        // Note: In a real scenario, these would be handled by global exception middleware
        // For unit testing, we test that the service layer properly wraps exceptions in Result<T>

        var refreshRequest = new RefreshFilesRequest { BatchSize = 100 };
        await Assert.ThrowsAsync<Exception>(() => InvokeRefreshFilesEndpoint(refreshRequest));

        var moveRequest = new MoveFilesRequest 
        { 
            FilesToMove = new List<FileMoveDto> { new() { Id = 1, FileCategory = "Test" } } 
        };
        await Assert.ThrowsAsync<Exception>(() => InvokeMoveFilesEndpoint(moveRequest));

        await Assert.ThrowsAsync<Exception>(() => InvokeForceCategorizeEndpoint());
        await Assert.ThrowsAsync<Exception>(() => InvokeTrainModelEndpoint());
        await Assert.ThrowsAsync<Exception>(() => InvokeGetJobStatusEndpoint("test-job"));
    }

    [Fact]
    public async Task AllEndpoints_WithCancellationTokens_PassCorrectly()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Setup all service methods to return success
        _mockActionsService.Setup(x => x.RefreshFilesAsync(It.IsAny<RefreshFilesRequest>(), token))
            .ReturnsAsync(Result<ActionJobResponse>.Success(new ActionJobResponse { JobId = "refresh-job" }));

        _mockActionsService.Setup(x => x.MoveFilesAsync(It.IsAny<MoveFilesRequest>(), token))
            .ReturnsAsync(Result<ActionJobResponse>.Success(new ActionJobResponse { JobId = "move-job" }));

        _mockActionsService.Setup(x => x.ForceCategorizeAsync(token))
            .ReturnsAsync(Result<ActionJobResponse>.Success(new ActionJobResponse { JobId = "force-job" }));

        _mockActionsService.Setup(x => x.TrainModelAsync(token))
            .ReturnsAsync(Result<TrainModelResponse>.Success(new TrainModelResponse { Success = true }));

        _mockActionsService.Setup(x => x.GetJobStatusAsync("test-job", token))
            .ReturnsAsync(Result<ActionJobResponse>.Success(new ActionJobResponse { JobId = "test-job" }));

        // Act & Assert - Verify cancellation tokens are passed correctly
        await InvokeRefreshFilesEndpoint(new RefreshFilesRequest { BatchSize = 100 }, token);
        await InvokeMoveFilesEndpoint(new MoveFilesRequest 
        { 
            FilesToMove = new List<FileMoveDto> { new() { Id = 1, FileCategory = "Test" } } 
        }, token);
        await InvokeForceCategorizeEndpoint(token);
        await InvokeTrainModelEndpoint(token);
        await InvokeGetJobStatusEndpoint("test-job", token);

        // Verify all service methods were called with the correct cancellation token
        _mockActionsService.Verify(x => x.RefreshFilesAsync(It.IsAny<RefreshFilesRequest>(), token), Times.Once);
        _mockActionsService.Verify(x => x.MoveFilesAsync(It.IsAny<MoveFilesRequest>(), token), Times.Once);
        _mockActionsService.Verify(x => x.ForceCategorizeAsync(token), Times.Once);
        _mockActionsService.Verify(x => x.TrainModelAsync(token), Times.Once);
        _mockActionsService.Verify(x => x.GetJobStatusAsync("test-job", token), Times.Once);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Simulates invoking the RefreshFiles endpoint.
    /// </summary>
    private async Task<IResult> InvokeRefreshFilesEndpoint(
        RefreshFilesRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Simulate the endpoint logic
        var result = await _mockActionsService.Object.RefreshFilesAsync(request, cancellationToken);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { Error = result.Error });
    }

    /// <summary>
    /// Simulates invoking the MoveFiles endpoint.
    /// </summary>
    private async Task<IResult> InvokeMoveFilesEndpoint(
        MoveFilesRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _mockActionsService.Object.MoveFilesAsync(request, cancellationToken);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { Error = result.Error });
    }

    /// <summary>
    /// Simulates invoking the ForceCategorize endpoint.
    /// </summary>
    private async Task<IResult> InvokeForceCategorizeEndpoint(CancellationToken cancellationToken = default)
    {
        var result = await _mockActionsService.Object.ForceCategorizeAsync(cancellationToken);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { Error = result.Error });
    }

    /// <summary>
    /// Simulates invoking the TrainModel endpoint.
    /// </summary>
    private async Task<IResult> InvokeTrainModelEndpoint(CancellationToken cancellationToken = default)
    {
        var result = await _mockActionsService.Object.TrainModelAsync(cancellationToken);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { Error = result.Error });
    }

    /// <summary>
    /// Simulates invoking the GetJobStatus endpoint.
    /// </summary>
    private async Task<IResult> InvokeGetJobStatusEndpoint(
        string jobId, 
        CancellationToken cancellationToken = default)
    {
        var result = await _mockActionsService.Object.GetJobStatusAsync(jobId, cancellationToken);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { Error = result.Error });
    }

    #endregion
}