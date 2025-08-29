namespace FileCategorization_App.Data.DTOs.Actions;

/// <summary>
/// Response DTO for train model operation.
/// </summary>
public class TrainModelResponse
{
    /// <summary>
    /// Job identifier for the training operation.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Status message of the training operation.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the training started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Model accuracy if training is completed.
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// Number of samples used for training.
    /// </summary>
    public int TrainingSamples { get; set; }

    /// <summary>
    /// Number of validation samples used.
    /// </summary>
    public int ValidationSamples { get; set; }

    /// <summary>
    /// Training duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }
}