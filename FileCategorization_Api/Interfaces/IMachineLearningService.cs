using FileCategorization_Api.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Interface for machine learning operations for file categorization.
/// </summary>
public interface IMachineLearningService
{
    /// <summary>
    /// Predicts the category of a single file based on its name.
    /// </summary>
    /// <param name="fileNameToPredict">The file name to categorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the predicted category.</returns>
    Task<Result<string>> PredictFileCategoryAsync(string fileNameToPredict, CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts the categories of multiple files based on their names.
    /// </summary>
    /// <param name="fileList">The list of files to categorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of files with updated categories.</returns>
    Task<Result<List<FilesDetail>>> PredictFileCategoriesAsync(List<FilesDetail> fileList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trains a new model and saves it to disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating the success of the training operation.</returns>
    Task<Result<string>> TrainAndSaveModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current model information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing model information.</returns>
    Task<Result<string>> GetModelInfoAsync(CancellationToken cancellationToken = default);
}