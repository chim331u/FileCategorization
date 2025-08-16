using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Domain.Entities.MachineLearning;
using FileCategorization_Api.Interfaces;
using Microsoft.ML;
using System.Collections.Concurrent;

namespace FileCategorization_Api.Services;

/// <summary>
/// Machine learning service for file categorization using ML.NET.
/// Implements thread-safe model caching and proper async patterns.
/// </summary>
public class MachineLearningService : IMachineLearningService, IDisposable
{
    private readonly ILogger<MachineLearningService> _logger;
    private readonly IConfigsService _configsService;
    private readonly MLContext _mlContext;
    private readonly SemaphoreSlim _modelLoadSemaphore;
    
    // Thread-safe caching
    private volatile ITransformer? _cachedModel;
    private volatile PredictionEngine<MlFileName, MlFileNamePrediction>? _predictionEngine;
    private volatile string? _currentModelPath;
    private DateTime _modelLoadTime;
    private bool _disposed;

    public MachineLearningService(ILogger<MachineLearningService> logger, IConfigsService configsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configsService = configsService ?? throw new ArgumentNullException(nameof(configsService));
        _mlContext = new MLContext(seed: 0);
        _modelLoadSemaphore = new SemaphoreSlim(1, 1);
        
        _logger.LogInformation("MachineLearningService initialized");
    }

    /// <inheritdoc/>
    public async Task<Result<string>> PredictFileCategoryAsync(string fileNameToPredict, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        try
        {
            if (string.IsNullOrWhiteSpace(fileNameToPredict))
            {
                return Result<string>.Failure("File name cannot be null or empty");
            }

            _logger.LogDebug("Predicting category for file: {FileName}", fileNameToPredict);

            var predictionEngine = await EnsureModelLoadedAsync(cancellationToken);
            if (predictionEngine.IsFailure)
            {
                return Result<string>.Failure($"Failed to load model: {predictionEngine.Error}");
            }

            var input = new MlFileName { FileName = fileNameToPredict };
            var prediction = predictionEngine.Value!.Predict(input);

            _logger.LogDebug("Predicted category for {FileName}: {Category}", fileNameToPredict, prediction.Area);
            
            return Result<string>.Success(prediction.Area ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting category for file: {FileName}", fileNameToPredict);
            return Result<string>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<List<FilesDetail>>> PredictFileCategoriesAsync(List<FilesDetail> fileList, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        try
        {
            if (fileList == null)
            {
                return Result<List<FilesDetail>>.Failure("File list cannot be null");
            }

            if (fileList.Count == 0)
            {
                return Result<List<FilesDetail>>.Success(fileList);
            }

            _logger.LogInformation("Predicting categories for {Count} files", fileList.Count);

            var predictionEngine = await EnsureModelLoadedAsync(cancellationToken);
            if (predictionEngine.IsFailure)
            {
                return Result<List<FilesDetail>>.Failure($"Failed to load model: {predictionEngine.Error}");
            }

            var engine = predictionEngine.Value!;
            var processedCount = 0;

            foreach (var file in fileList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(file.Name))
                {
                    try
                    {
                        var input = new MlFileName { FileName = file.Name };
                        var prediction = engine.Predict(input);
                        file.FileCategory = prediction.Area ?? "Unknown";
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to predict category for file: {FileName}", file.Name);
                        file.FileCategory = "Unknown";
                    }
                }
                else
                {
                    file.FileCategory = "Unknown";
                }
            }

            _logger.LogInformation("Successfully predicted categories for {ProcessedCount}/{TotalCount} files", 
                processedCount, fileList.Count);

            return Result<List<FilesDetail>>.Success(fileList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting categories for file list");
            return Result<List<FilesDetail>>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> TrainAndSaveModelAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        await _modelLoadSemaphore.WaitAsync(cancellationToken);
        try
        {
            
            _logger.LogInformation("Starting model training");

            var configResult = await GetModelConfigurationAsync(cancellationToken);
            if (configResult.IsFailure)
            {
                return Result<string>.Failure($"Failed to get configuration: {configResult.Error}");
            }

            var config = configResult.Value!;
            var trainDataPath = Path.Combine(config.TrainDataPath, config.TrainDataName);

            if (!File.Exists(trainDataPath))
            {
                return Result<string>.Failure($"Training data file not found: {trainDataPath}");
            }

            _logger.LogInformation("Loading training data from: {TrainDataPath}", trainDataPath);

            IDataView trainingDataView;
            try
            {
                trainingDataView = _mlContext.Data.LoadFromTextFile<MlFileName>(trainDataPath, hasHeader: true, separatorChar: ';');
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to load training data: {ex.Message}");
            }

            var pipeline = BuildTrainingPipeline();
            var trainingPipeline = pipeline
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _logger.LogInformation("Training model...");
            
            ITransformer trainedModel;
            try
            {
                trainedModel = trainingPipeline.Fit(trainingDataView);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Model training failed: {ex.Message}");
            }

            var modelPath = Path.Combine(config.ModelPath, config.ModelName);
            
            // Ensure model directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

            try
            {
                _mlContext.Model.Save(trainedModel, trainingDataView.Schema, modelPath);
                _logger.LogInformation("Model saved successfully to: {ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to save model: {ex.Message}");
            }

            // Clear cached model to force reload
            InvalidateModelCache();

            var message = $"Model training completed successfully. Model saved to: {modelPath}";
            _logger.LogInformation(message);
            
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model training");
            return Result<string>.FromException(ex);
        }
        finally
        {
            _modelLoadSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> GetModelInfoAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        try
        {

            var configResult = await GetModelConfigurationAsync(cancellationToken);
            if (configResult.IsFailure)
            {
                return Result<string>.Failure($"Failed to get configuration: {configResult.Error}");
            }

            var config = configResult.Value!;
            var modelPath = Path.Combine(config.ModelPath, config.ModelName);

            if (!File.Exists(modelPath))
            {
                return Result<string>.Success("Model not found - will be trained on first prediction");
            }

            var fileInfo = new FileInfo(modelPath);
            var modelInfo = $"Model exists at: {modelPath}, Size: {fileInfo.Length} bytes, Last Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            
            if (_cachedModel != null)
            {
                modelInfo += $", Model cached since: {_modelLoadTime:yyyy-MM-dd HH:mm:ss}";
            }

            return Result<string>.Success(modelInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info");
            return Result<string>.FromException(ex);
        }
    }

    #region Private Methods

    /// <summary>
    /// Ensures the ML model is loaded and returns a cached prediction engine.
    /// Thread-safe implementation with double-check locking pattern.
    /// </summary>
    private async Task<Result<PredictionEngine<MlFileName, MlFileNamePrediction>>> EnsureModelLoadedAsync(CancellationToken cancellationToken)
    {
        // Fast path - check if we already have a cached engine
        if (_predictionEngine != null && _cachedModel != null)
        {
            return Result<PredictionEngine<MlFileName, MlFileNamePrediction>>.Success(_predictionEngine);
        }

        await _modelLoadSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_predictionEngine != null && _cachedModel != null)
            {
                return Result<PredictionEngine<MlFileName, MlFileNamePrediction>>.Success(_predictionEngine);
            }

            var configResult = await GetModelConfigurationAsync(cancellationToken);
            if (configResult.IsFailure)
            {
                return Result<PredictionEngine<MlFileName, MlFileNamePrediction>>.Failure(configResult.Error!);
            }

            var config = configResult.Value!;
            var modelPath = Path.Combine(config.ModelPath, config.ModelName);

            // Check if model exists, if not, train it
            if (!File.Exists(modelPath))
            {
                _logger.LogInformation("Model not found at {ModelPath}, training new model", modelPath);
                var trainResult = await TrainAndSaveModelAsync(cancellationToken);
                if (trainResult.IsFailure)
                {
                    return Result<PredictionEngine<MlFileName, MlFileNamePrediction>>.Failure($"Failed to train model: {trainResult.Error}");
                }
            }

            try
            {
                _cachedModel = _mlContext.Model.Load(modelPath, out _);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MlFileName, MlFileNamePrediction>(_cachedModel);
                _currentModelPath = modelPath;
                _modelLoadTime = DateTime.UtcNow;

                _logger.LogInformation("Model loaded successfully from: {ModelPath}", modelPath);
                return Result<PredictionEngine<MlFileName, MlFileNamePrediction>>.Success(_predictionEngine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model from: {ModelPath}", modelPath);
                return Result<PredictionEngine<MlFileName, MlFileNamePrediction>>.FromException(ex);
            }
        }
        finally
        {
            _modelLoadSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the model configuration from the config service.
    /// </summary>
    private async Task<Result<ModelConfiguration>> GetModelConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var modelPath = await _configsService.GetConfigValue("MODELPATH");
            var modelName = await _configsService.GetConfigValue("MODELNAME");
            var trainDataPath = await _configsService.GetConfigValue("TRAINDATAPATH");
            var trainDataName = await _configsService.GetConfigValue("TRAINDATANAME");

            if (string.IsNullOrWhiteSpace(modelPath) || string.IsNullOrWhiteSpace(modelName))
            {
                return Result<ModelConfiguration>.Failure("Model configuration is incomplete. Required: MODELPATH, MODELNAME");
            }

            if (string.IsNullOrWhiteSpace(trainDataPath) || string.IsNullOrWhiteSpace(trainDataName))
            {
                return Result<ModelConfiguration>.Failure("Training data configuration is incomplete. Required: TRAINDATAPATH, TRAINDATANAME");
            }

            var config = new ModelConfiguration
            {
                ModelPath = modelPath,
                ModelName = modelName,
                TrainDataPath = trainDataPath,
                TrainDataName = trainDataName
            };

            return Result<ModelConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model configuration");
            return Result<ModelConfiguration>.FromException(ex);
        }
    }

    /// <summary>
    /// Builds the ML.NET training pipeline.
    /// </summary>
    private IEstimator<ITransformer> BuildTrainingPipeline()
    {
        return _mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "Area", outputColumnName: "Label")
            .Append(_mlContext.Transforms.Text.FeaturizeText(inputColumnName: "FileName", outputColumnName: "FileNameFeaturized"))
            .Append(_mlContext.Transforms.Concatenate("Features", "FileNameFeaturized"))
            .AppendCacheCheckpoint(_mlContext);
    }

    /// <summary>
    /// Invalidates the cached model, forcing a reload on next prediction.
    /// </summary>
    private void InvalidateModelCache()
    {
        _predictionEngine?.Dispose();
        _predictionEngine = null;
        _cachedModel = null;
        _currentModelPath = null;
        _logger.LogDebug("Model cache invalidated");
    }

    /// <summary>
    /// Throws if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MachineLearningService));
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _predictionEngine?.Dispose();
                _modelLoadSemaphore?.Dispose();
                _logger.LogInformation("MachineLearningService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MachineLearningService");
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Configuration for ML model paths and settings.
    /// </summary>
    private class ModelConfiguration
    {
        public string ModelPath { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string TrainDataPath { get; set; } = string.Empty;
        public string TrainDataName { get; set; } = string.Empty;
    }

    #endregion
}
