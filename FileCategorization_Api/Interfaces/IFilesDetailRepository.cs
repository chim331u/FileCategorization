using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Repository interface for FilesDetail entity operations.
/// </summary>
public interface IFilesDetailRepository : IRepository<FilesDetail>
{
    /// <summary>
    /// Gets files by their category.
    /// </summary>
    /// <param name="category">The file category to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of files in the specified category.</returns>
    Task<Result<IEnumerable<FilesDetail>>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all files that need to be categorized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of files that need categorization.</returns>
    Task<Result<IEnumerable<FilesDetail>>> GetToCategorizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct file categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of distinct categories.</returns>
    Task<Result<IEnumerable<string>>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files that are marked as new.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of new files.</returns>
    Task<Result<IEnumerable<FilesDetail>>> GetNewFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files filtered by various criteria.
    /// </summary>
    /// <param name="filterType">The filter type (1=All, 2=Categorized, 3=ToCategorize, 4=New).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of filtered files.</returns>
    Task<Result<IEnumerable<FilesDetail>>> GetFilteredFilesAsync(int filterType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files that should not be moved.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of files that should not be moved.</returns>
    Task<Result<IEnumerable<FilesDetail>>> GetNotToMoveFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest file for each category (used in GetLastView functionality).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of latest files per category.</returns>
    Task<Result<IEnumerable<FilesDetail>>> GetLatestFilesByCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the categorization status of a file.
    /// </summary>
    /// <param name="fileId">The file identifier.</param>
    /// <param name="category">The new category.</param>
    /// <param name="isToCategorize">Whether the file still needs categorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully.</returns>
    Task<Result<bool>> UpdateCategorizationAsync(int fileId, string category, bool isToCategorize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches files by name pattern.
    /// </summary>
    /// <param name="namePattern">The name pattern to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of files matching the pattern.</returns>
    Task<Result<IEnumerable<FilesDetail>>> SearchByNameAsync(string namePattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a file's LastUpdate to current time and sets IsNotToMove to false.
    /// </summary>
    /// <param name="fileId">The file identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully, false if file not found.</returns>
    Task<Result<bool>> UpdateNotShowAgainAsync(int fileId, CancellationToken cancellationToken = default);
}