using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation providing common CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type that inherits from BaseEntity.</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public Repository(ApplicationContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    /// <summary>
    /// Gets an entity by its unique identifier.
    /// </summary>
    public virtual async Task<Result<T?>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate ID parameter
            if (id <= 0)
            {
                _logger.LogWarning("Invalid ID provided: {Id}", id);
                return Result<T?>.Success(null); // Return null for invalid IDs
            }

            var entity = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive, cancellationToken);

            return Result<T?>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            return Result<T?>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets all entities of type T.
    /// </summary>
    public virtual async Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbSet
                .AsNoTracking()
                .Where(e => e.IsActive)
                .OrderBy(e => e.Id)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<T>>.Success(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all entities of type {EntityType}", typeof(T).Name);
            return Result<IEnumerable<T>>.FromException(ex);
        }
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    public virtual async Task<Result<T>> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                return Result<T>.Failure("Entity cannot be null");

            // Set audit fields
            entity.CreatedDate = DateTime.UtcNow;
            entity.LastUpdatedDate = DateTime.UtcNow;
            entity.IsActive = true;
            

            _dbSet.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added new entity of type {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
            return Result<T>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
            return Result<T>.FromException(ex);
        }
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    public virtual async Task<Result<T>> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                return Result<T>.Failure("Entity cannot be null");

            _logger.LogInformation("Repository UpdateAsync called with entity ID: {EntityId}", entity.Id);
            
            var existingEntity = await _dbSet.FindAsync(new object[] { entity.Id }, cancellationToken);
            if (existingEntity == null)
            {
                _logger.LogError("Existing entity not found with ID: {EntityId}", entity.Id);
                return Result<T>.Failure($"Entity with ID {entity.Id} not found");
            }

            // Update audit fields
            entity.LastUpdatedDate = DateTime.UtcNow;

            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated entity of type {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
            return Result<T>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity of type {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
            return Result<T>.FromException(ex);
        }
    }

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    public virtual async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null)
                return Result<bool>.Failure($"Entity with ID {id} not found");

            // Soft delete by setting IsActive to false
            entity.IsActive = false;
            entity.LastUpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Soft deleted entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            return Result<bool>.FromException(ex);
        }
    }

    /// <summary>
    /// Checks if an entity exists by its identifier.
    /// </summary>
    public virtual async Task<Result<bool>> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _dbSet
                .AsNoTracking()
                .AnyAsync(e => e.Id == id && e.IsActive, cancellationToken);

            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            return Result<bool>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets the total count of active entities.
    /// </summary>
    public virtual async Task<Result<int>> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _dbSet
                .AsNoTracking()
                .Where(e => e.IsActive)
                .CountAsync(cancellationToken);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
            return Result<int>.FromException(ex);
        }
    }
}