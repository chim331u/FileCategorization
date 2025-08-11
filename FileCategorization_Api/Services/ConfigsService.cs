using FileCategorization_Api.AppContext;
using FileCategorization_Api.Contracts.Configs;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Models.FileCategorization;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Services;

/// <summary>
/// Provides methods for managing application configurations.
/// </summary>
public class ConfigsService : IConfigsService
{
    private readonly ApplicationContext _context; // Database context
    private readonly ILogger<ConfigsService> _logger; // Logger for logging information and errors
    private readonly IConfiguration _config;
    private readonly IUtilityServices _utilityServices;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigsService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    /// <param name="config">The application configuration.</param>
    /// <param name="utilityServices">The utility services.</param>
    /// <param name="environment">The host environment.</param>
    public ConfigsService(ApplicationContext context, ILogger<ConfigsService> logger, IConfiguration config,
        IUtilityServices utilityServices, IHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _config = config;
        _environment = environment;
        _utilityServices = utilityServices;
    }

    /// <summary>
    /// Retrieves the list of active configurations.
    /// </summary>
    /// <returns>A list of active configurations filtered by the environment.</returns>
    public async Task<IEnumerable<ConfigsDto>> GetConfigList()
    {
        try
        {
            var configsList = await _context.Configuration.AsNoTracking()
                .OrderBy(x => x.Id)
                .Where(x => x.IsActive && x.IsDev == _environment.IsDevelopment())
                .ToListAsync();

            return configsList.Select(configsDto => new ConfigsDto
            {
                Id = configsDto.Id,
                Value = configsDto.Value,
                Key = configsDto.Key
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving config list: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Retrieves the value of a configuration by its key.
    /// </summary>
    /// <param name="key">The key of the configuration.</param>
    /// <returns>The value of the configuration.</returns>
    public Task<string> GetConfigValue(string key)
    {
        try
        {
            var configValue = _context.Configuration
                .Single(x => x.IsActive && x.IsDev == _environment.IsDevelopment() && x.Key == key).Value!
                .ToString();

            return Task.FromResult(configValue);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving config value: {ex.Message}");
            return Task.FromResult<string>(null!);
        }
    }

    /// <summary>
    /// Retrieves a configuration by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the configuration.</param>
    /// <returns>The configuration details.</returns>
    public async Task<ConfigsDto> GetConfig(int id)
    {
        try
        {
            var config = await _context.Configuration.FindAsync(id);

            if (config != null)
                return new ConfigsDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value
                };
            _logger.LogWarning($"Config with ID {id} not found.");
            return null!;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving config: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Adds a new configuration.
    /// </summary>
    /// <param name="config">The configuration data to add.</param>
    /// <returns>The details of the added configuration.</returns>
    public async Task<ConfigsDto> AddConfig(ConfigsDto config)
    {
        try
        {
            var newConfig = new Configs
            {
                CreatedDate = DateTime.Now,
                IsActive = true,
                Key = config.Key,
                Value = config.Value,
                IsDev = _environment.IsDevelopment()
            };

            _context.Configuration.Add(newConfig);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Config added successfully.");

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding config: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Updates an existing configuration.
    /// </summary>
    /// <param name="id">The unique identifier of the configuration to update.</param>
    /// <param name="config">The updated configuration data.</param>
    /// <returns>The details of the updated configuration.</returns>
    public async Task<ConfigsDto> UpdateConfig(int id, ConfigsDto config)
    {
        try
        {
            var existingConfig = await _context.Configuration.FindAsync(id);
            if (existingConfig == null)
            {
                _logger.LogWarning($"Config with ID {id} not found.");
                return null!;
            }

            existingConfig.Key = config.Key;
            existingConfig.Value = config.Value;
            existingConfig.LastUpdatedDate = DateTime.Now;

            _context.Configuration.Update(existingConfig);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Config updated successfully.");

            return new ConfigsDto
            {
                Id = existingConfig.Id,
                Key = existingConfig.Key,
                Value = existingConfig.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating config: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Deletes an existing configuration by marking it as inactive.
    /// </summary>
    /// <param name="id">The unique identifier of the configuration to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> DeleteConfig(int id)
    {
        try
        {
            var existingConfig = await _context.Configuration.FindAsync(id);
            if (existingConfig == null)
            {
                _logger.LogWarning($"Config with ID {id} not found.");
                return false;
            }

            existingConfig.IsActive = false;
            existingConfig.LastUpdatedDate = DateTime.Now;

            _context.Configuration.Update(existingConfig);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Config deleted successfully.");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting config: {ex.Message}");
            return false;
        }
    }


}