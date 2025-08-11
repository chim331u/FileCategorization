using FileCategorization_Api.Contracts.Config;
using FileCategorization_Api.Core.Common;

namespace FileCategorization_Api.Application.Services;

/// <summary>
/// Defines the contract for configuration query service operations.
/// </summary>
public interface IConfigQueryService
{
    /// <summary>
    /// Gets all configurations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the configurations.</returns>
    Task<Result<IEnumerable<ConfigResponse>>> GetAllConfigsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration by its ID.
    /// </summary>
    /// <param name="id">The configuration ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the configuration if found.</returns>
    Task<Result<ConfigResponse?>> GetConfigByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration by its key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the configuration if found.</returns>
    Task<Result<ConfigResponse?>> GetConfigByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value of a configuration by its key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the configuration value if found.</returns>
    Task<Result<string?>> GetConfigValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configurations filtered by environment.
    /// </summary>
    /// <param name="isDev">Whether to get development or production configurations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the filtered configurations.</returns>
    Task<Result<IEnumerable<ConfigResponse>>> GetConfigsByEnvironmentAsync(bool isDev, CancellationToken cancellationToken = default);
}