using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Infrastructure.Data.Repositories;

/// <summary>
/// Implementation of configuration repository.
/// </summary>
public class ConfigRepository : Repository<Configs>, IConfigRepository
{
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the ConfigRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="environment">The host environment.</param>
    public ConfigRepository(ApplicationContext context, ILogger<ConfigRepository> logger, IHostEnvironment environment)
        : base(context, logger)
    {
        _environment = environment;
    }

    /// <inheritdoc/>
    public async Task<Result<Configs?>> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving configuration by key: {Key}", key);

            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Attempted to retrieve configuration with null or empty key");
                return Result<Configs?>.Failure("Configuration key cannot be null or empty");
            }

            var config = await _dbSet.AsNoTracking()
                .Where(c => c.IsActive && c.Key == key)
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogInformation("Configuration retrieval by key completed. Found: {Found}", config != null);
            return Result<Configs?>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration by key: {Key}", key);
            return Result<Configs?>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string?>> GetValueByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving configuration value by key: {Key}", key);

            var configResult = await GetByKeyAsync(key, cancellationToken);
            if (configResult.IsFailure)
            {
                return Result<string?>.Failure(configResult.Error!);
            }

            var value = configResult.Value?.Value;
            _logger.LogInformation("Configuration value retrieval completed. Has value: {HasValue}", !string.IsNullOrEmpty(value));
            return Result<string?>.Success(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration value by key: {Key}", key);
            return Result<string>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> KeyExistsAsync(string key, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking if configuration key exists: {Key}", key);

            if (string.IsNullOrWhiteSpace(key))
            {
                return Result<bool>.Success(false);
            }

            var query = _dbSet.AsNoTracking()
                .Where(c => c.IsActive && c.Key == key);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);

            _logger.LogInformation("Configuration key existence check completed. Exists: {Exists}", exists);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking configuration key existence: {Key}", key);
            return Result<bool>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<Configs>>> GetByEnvironmentAsync(bool isDev, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving configurations by environment. IsDev: {IsDev}", isDev);

            var configs = await _dbSet.AsNoTracking()
                .Where(c => c.IsActive && c.IsDev == isDev)
                .OrderBy(c => c.Key)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} configurations for environment IsDev: {IsDev}", configs.Count, isDev);
            return Result<IEnumerable<Configs>>.Success(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations by environment. IsDev: {IsDev}", isDev);
            return Result<IEnumerable<Configs>>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> UpdateEnvironmentAsync(int id, bool isDev, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating configuration environment. Id: {Id}, IsDev: {IsDev}", id, isDev);

            var config = await _dbSet
                .Where(c => c.Id == id && c.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (config == null)
            {
                _logger.LogWarning("Configuration not found for environment update. Id: {Id}", id);
                return Result<bool>.Success(false);
            }

            config.IsDev = isDev;
            config.LastUpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Configuration environment updated successfully. Id: {Id}", id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration environment. Id: {Id}", id);
            return Result<bool>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public override async Task<Result<IEnumerable<Configs>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all active configurations");

            var configs = await _dbSet.AsNoTracking()
                .Where(c => c.IsActive && c.IsDev == _environment.IsDevelopment())
                .OrderBy(c => c.Key)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} configurations", configs.Count);
            return Result<IEnumerable<Configs>>.Success(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configurations");
            return Result<IEnumerable<Configs>>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public override async Task<Result<Configs>> AddAsync(Configs entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding new configuration with key: {Key}", entity.Key);

            // Check if key already exists
            var keyExistsResult = await KeyExistsAsync(entity.Key!, cancellationToken: cancellationToken);
            if (keyExistsResult.IsFailure)
            {
                return Result<Configs>.Failure($"Error checking key existence: {keyExistsResult.Error}");
            }

            if (keyExistsResult.Value)
            {
                _logger.LogWarning("Configuration key already exists: {Key}", entity.Key);
                return Result<Configs>.Failure($"Configuration key '{entity.Key}' already exists");
            }

            // Set audit fields
            entity.CreatedDate = DateTime.UtcNow;
            entity.LastUpdatedDate = DateTime.UtcNow;
            entity.IsActive = true;

            return await base.AddAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding configuration with key: {Key}", entity.Key);
            return Result<Configs>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public override async Task<Result<Configs>> UpdateAsync(Configs entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating configuration with ID: {Id}, Key: {Key}", entity.Id, entity.Key);

            // Key uniqueness check removed for updates - allow updating existing keys

            // Update audit fields
            entity.LastUpdatedDate = DateTime.UtcNow;

            return await base.UpdateAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration with ID: {Id}", entity.Id);
            return Result<Configs>.FromException(ex);
        }
    }
}