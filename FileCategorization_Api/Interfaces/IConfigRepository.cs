using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Defines the contract for configuration repository operations.
/// </summary>
public interface IConfigRepository : IRepository<Configs>
{
    /// <summary>
    /// Gets a configuration by its key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the configuration if found.</returns>
    Task<Result<Configs?>> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value of a configuration by its key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the configuration value if found.</returns>
    Task<Result<string?>> GetValueByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a configuration key already exists.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the key exists.</returns>
    Task<Result<bool>> KeyExistsAsync(string key, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active configurations filtered by environment.
    /// </summary>
    /// <param name="isDev">Whether to get development or production configurations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the filtered configurations.</returns>
    Task<Result<IEnumerable<Configs>>> GetByEnvironmentAsync(bool isDev, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration category status.
    /// </summary>
    /// <param name="id">The configuration ID.</param>
    /// <param name="isDev">The development environment flag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> UpdateEnvironmentAsync(int id, bool isDev, CancellationToken cancellationToken = default);
}