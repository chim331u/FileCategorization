using FileCategorization_Api.Common;
using FileCategorization_Api.Domain.Entities.DD;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Repository interface for DD-related operations with Result Pattern
/// </summary>
public interface IDDRepository
{
    // Thread operations
    Task<Result<DD_Threads?>> GetThreadByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Result<DD_Threads?>> GetThreadByIdAsync(int threadId, CancellationToken cancellationToken = default);
    Task<Result<List<DD_Threads>>> GetActiveThreadsAsync(CancellationToken cancellationToken = default);
    Task<Result<DD_Threads>> CreateThreadAsync(DD_Threads thread, CancellationToken cancellationToken = default);
    Task<Result<DD_Threads>> UpdateThreadAsync(DD_Threads thread, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeactivateThreadAsync(int threadId, CancellationToken cancellationToken = default);

    // Link operations
    Task<Result<List<DD_LinkEd2k>>> GetLinksByThreadIdAsync(int threadId, CancellationToken cancellationToken = default);
    Task<Result<DD_LinkEd2k?>> GetLinkByIdAsync(int linkId, CancellationToken cancellationToken = default);
    Task<Result<List<DD_LinkEd2k>>> GetExistingLinksAsync(List<string> ed2kLinks, int threadId, CancellationToken cancellationToken = default);
    Task<Result<int>> CreateLinksAsync(List<DD_LinkEd2k> links, CancellationToken cancellationToken = default);
    Task<Result<int>> UpdateLinksAsync(List<DD_LinkEd2k> links, CancellationToken cancellationToken = default);
    Task<Result<DD_LinkEd2k>> MarkLinkAsUsedAsync(int linkId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeactivateLinksAsync(List<int> linkIds, CancellationToken cancellationToken = default);

    // Statistics and reporting
    Task<Result<int>> GetNewLinksCountAsync(int threadId, CancellationToken cancellationToken = default);
    Task<Result<Dictionary<int, int>>> GetThreadsLinkCountsAsync(List<int> threadIds, CancellationToken cancellationToken = default);
}