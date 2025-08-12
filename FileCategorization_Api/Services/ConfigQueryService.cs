using AutoMapper;
using FileCategorization_Api.Services;
using FileCategorization_Api.Domain.Entities.Config;
using FileCategorization_Api.Common;
using FileCategorization_Api.Interfaces;

namespace FileCategorization_Api.Services;

/// <summary>
/// Implementation of configuration query service.
/// </summary>
public class ConfigQueryService : IConfigQueryService
{
    private readonly IConfigRepository _configRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ConfigQueryService> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigQueryService class.
    /// </summary>
    /// <param name="configRepository">The configuration repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <param name="logger">The logger instance.</param>
    public ConfigQueryService(
        IConfigRepository configRepository,
        IMapper mapper,
        ILogger<ConfigQueryService> logger)
    {
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ConfigResponse>>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all configurations");

        var configsResult = await _configRepository.GetAllAsync(cancellationToken);
        if (configsResult.IsFailure)
        {
            return Result<IEnumerable<ConfigResponse>>.Failure(configsResult.ErrorMessage!);
        }

        var responseList = _mapper.Map<IEnumerable<ConfigResponse>>(configsResult.Data);
        return Result<IEnumerable<ConfigResponse>>.Success(responseList);
    }

    /// <inheritdoc/>
    public async Task<Result<ConfigResponse?>> GetConfigByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving configuration by ID: {Id}", id);

        if (id <= 0)
        {
            _logger.LogWarning("Invalid configuration ID provided: {Id}", id);
            return Result<ConfigResponse?>.Failure("Configuration ID must be greater than zero");
        }

        var configResult = await _configRepository.GetByIdAsync(id, cancellationToken);
        if (configResult.IsFailure)
        {
            return Result<ConfigResponse?>.Failure(configResult.ErrorMessage!);
        }

        if (configResult.Data == null)
        {
            _logger.LogInformation("Configuration not found with ID: {Id}", id);
            return Result<ConfigResponse?>.Success(null);
        }

        var response = _mapper.Map<ConfigResponse>(configResult.Data);
        return Result<ConfigResponse?>.Success(response);
    }

    /// <inheritdoc/>
    public async Task<Result<ConfigResponse?>> GetConfigByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving configuration by key: {Key}", key);

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Empty or null configuration key provided");
            return Result<ConfigResponse?>.Failure("Configuration key cannot be null or empty");
        }

        var configResult = await _configRepository.GetByKeyAsync(key, cancellationToken);
        if (configResult.IsFailure)
        {
            return Result<ConfigResponse?>.Failure(configResult.ErrorMessage!);
        }

        if (configResult.Data == null)
        {
            _logger.LogInformation("Configuration not found with key: {Key}", key);
            return Result<ConfigResponse?>.Success(null);
        }

        var response = _mapper.Map<ConfigResponse>(configResult.Data);
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
            return Result<string?>.Failure(valueResult.ErrorMessage!);
        }

        return Result<string?>.Success(valueResult.Data);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ConfigResponse>>> GetConfigsByEnvironmentAsync(bool isDev, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving configurations by environment. IsDev: {IsDev}", isDev);

        var configsResult = await _configRepository.GetByEnvironmentAsync(isDev, cancellationToken);
        if (configsResult.IsFailure)
        {
            return Result<IEnumerable<ConfigResponse>>.Failure(configsResult.ErrorMessage!);
        }

        var responseList = _mapper.Map<IEnumerable<ConfigResponse>>(configsResult.Data);
        return Result<IEnumerable<ConfigResponse>>.Success(responseList);
    }
}