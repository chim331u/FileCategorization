using FileCategorization_Shared.DTOs.FileManagement;

namespace FileCategorization_Api.Interfaces;

public interface IHangFireJobService
{
    Task ExecuteAsync(string fileName, string destinationFolder, CancellationToken cancellationToken);

    Task MoveFilesJob(List<FileMoveDto> filesToMove, CancellationToken cancellationToken);

    Task RefreshFiles(CancellationToken cancellationToken);

    /// <summary>
    /// Background job for force categorization of uncategorized files with progress tracking.
    /// Executes ML categorization for files that haven't been categorized yet and provides detailed feedback via SignalR.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for job cancellation</param>
    Task ForceCategorizeJob(CancellationToken cancellationToken);

    /// <summary>
    /// Background job for machine learning model training with comprehensive progress tracking.
    /// Executes ML model training asynchronously and provides detailed feedback via SignalR.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for job cancellation</param>
    Task TrainModelJob(CancellationToken cancellationToken);
}