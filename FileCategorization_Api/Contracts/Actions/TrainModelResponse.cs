namespace FileCategorization_Api.Contracts.Actions;

/// <summary>
/// Response DTO for train model operation.
/// </summary>
public class TrainModelResponse
{
    /// <summary>
    /// Whether the training was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Path where the trained model was saved.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Size of the trained model file in bytes.
    /// </summary>
    public long? ModelSizeBytes { get; set; }

    /// <summary>
    /// Number of training samples used.
    /// </summary>
    public int TrainingSamples { get; set; }

    /// <summary>
    /// Training duration.
    /// </summary>
    public TimeSpan TrainingDuration { get; set; }

    /// <summary>
    /// Model accuracy metrics if available.
    /// </summary>
    public Dictionary<string, double>? Metrics { get; set; }

    /// <summary>
    /// Model version identifier.
    /// </summary>
    public string ModelVersion { get; set; } = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
}