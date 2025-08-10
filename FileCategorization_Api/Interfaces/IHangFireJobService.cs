using FileCategorization_Api.Contracts.FilesDetail;

namespace FileCategorization_Api.Interfaces;

public interface IHangFireJobService
{
    Task ExecuteAsync(string fileName, string destinationFolder, CancellationToken cancellationToken);

    Task MoveFilesJob(List<FileMoveDto> filesToMove, CancellationToken cancellationToken);

    Task RefreshFiles(CancellationToken cancellationToken);

}