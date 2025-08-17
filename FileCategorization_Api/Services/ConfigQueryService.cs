using AutoMapper;
using FileCategorization_Api.Services;
using FileCategorization_Shared.DTOs.Configuration;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Interfaces;

namespace FileCategorization_Api.Services;

/// <summary>
/// Implementation of configuration query service.
/// Automatically filters configurations based on current environment (development/production).
/// </summary>
public class ConfigQueryService : IConfigQueryService
{
    private readonly IConfigRepository _configRepository;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ConfigQueryService> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigQueryService class.
    /// </summary>
    /// <param name="configRepository">The configuration repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <param name="environment">The host environment instance.</param>
    /// <param name="logger">The logger instance.</param>
    public ConfigQueryService(
        IConfigRepository configRepository,
        IMapper mapper,
        IHostEnvironment environment,
        ILogger<ConfigQueryService> logger)
    {
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ConfigResponse>>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        var isDev = _environment.IsDevelopment();
        _logger.LogInformation("Retrieving all configurations for environment: {Environment} (IsDev: {IsDev})", 
            _environment.EnvironmentName, isDev);

        // Use the environment-specific method from repository
        var configsResult = await _configRepository.GetByEnvironmentAsync(isDev, cancellationToken);
        if (configsResult.IsFailure)
        {
            return Result<IEnumerable<ConfigResponse>>.Failure(configsResult.Error!);
        }

        var responseList = _mapper.Map<IEnumerable<ConfigResponse>>(configsResult.Value);
        _logger.LogInformation("Retrieved {Count} configurations for environment {Environment}", 
            responseList.Count(), _environment.EnvironmentName);
        return Result<IEnumerable<ConfigResponse>>.Success(responseList);
    }

    /// <inheritdoc/>
    public async Task<Result<ConfigResponse?>> GetConfigByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var isDev = _environment.IsDevelopment();
        _logger.LogInformation("Retrieving configuration by ID: {Id} for environment: {Environment}", id, _environment.EnvironmentName);

        if (id <= 0)
        {
            _logger.LogWarning("Invalid configuration ID provided: {Id}", id);
            return Result<ConfigResponse?>.Failure("Configuration ID must be greater than zero");
        }

        var configResult = await _configRepository.GetByIdAsync(id, cancellationToken);
        if (configResult.IsFailure)
        {
            return Result<ConfigResponse?>.Failure(configResult.Error!);
        }

        if (configResult.Value == null || configResult.Value.IsDev != isDev)
        {
            _logger.LogInformation("Configuration not found with ID: {Id} for current environment", id);
            return Result<ConfigResponse?>.Success(null);
        }

        var response = _mapper.Map<ConfigResponse>(configResult.Value);
        return Result<ConfigResponse?>.Success(response);
    }

    /// <inheritdoc/>
    public async Task<Result<ConfigResponse?>> GetConfigByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var isDev = _environment.IsDevelopment();
        _logger.LogInformation("Retrieving configuration by key: {Key} for environment: {Environment}", key, _environment.EnvironmentName);

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Empty or null configuration key provided");
            return Result<ConfigResponse?>.Failure("Configuration key cannot be null or empty");
        }

        var configResult = await _configRepository.GetByKeyAsync(key, cancellationToken);
        if (configResult.IsFailure)
        {
            return Result<ConfigResponse?>.Failure(configResult.Error!);
        }

        if (configResult.Value == null || configResult.Value.IsDev != isDev)
        {
            _logger.LogInformation("Configuration not found with key: {Key} for current environment", key);
            return Result<ConfigResponse?>.Success(null);
        }

        var response = _mapper.Map<ConfigResponse>(configResult.Value);
        return Result<ConfigResponse?>.Success(response);
    }

    /// <inheritdoc/>
    public async Task<Result<string?>> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving configuration value by key: {Key}", key);

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Empty or null configuration key provided for value retrieval");
            return Result<string?>.Failure("Configuration key cannot be null or empty");
        }

        var valueResult = await _configRepository.GetValueByKeyAsync(key, cancellationToken);
        if (valueResult.IsFailure)
        {
            return Result<string?>.Failure(valueResult.Error!);
        }

        return Result<string?>.Success(valueResult.Value);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ConfigResponse>>> GetConfigsByEnvironmentAsync(bool isDev, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving configurations by environment. IsDev: {IsDev}", isDev);

        var configsResult = await _configRepository.GetByEnvironmentAsync(isDev, cancellationToken);
        if (configsResult.IsFailure)
        {
            return Result<IEnumerable<ConfigResponse>>.Failure(configsResult.Error!);
        }

        var responseList = _mapper.Map<IEnumerable<ConfigResponse>>(configsResult.Value);
        return Result<IEnumerable<ConfigResponse>>.Success(responseList);
    }
}