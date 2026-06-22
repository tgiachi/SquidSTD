using System.Linq.Expressions;
using FreeSql;
using SquidStd.Database.Abstractions.Data;
using SquidStd.Database.Abstractions.Data.Entities;

namespace SquidStd.Database.Abstractions.Interfaces.Data;

/// <summary>
/// Generic data access for a <see cref="BaseEntity"/> type: CRUD, bulk, and querying.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IDataAccess<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>Inserts a single entity (assigns Id/Created/Updated) inside a transaction.</summary>
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Gets an entity by its identifier, or null if not found.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an entity (bumps Updated) inside a transaction.</summary>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entity by id. Returns true if a row was removed.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the given entity. Returns true if a row was removed.</summary>
    Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Counts entities matching the optional predicate.</summary>
    Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Returns true if any entity matches the predicate.</summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Bulk-inserts entities inside a transaction. Returns affected rows.</summary>
    Task<int> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>Bulk-updates entities inside a transaction. Returns affected rows.</summary>
    Task<int> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>Bulk-deletes entities matching the predicate inside a transaction. Returns affected rows.</summary>
    Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Returns a composable, SQL-translated query (FreeSql ISelect) over the entity set.</summary>
    ISelect<TEntity> Query();

    /// <summary>Materializes entities matching the optional predicate.</summary>
    Task<IReadOnlyList<TEntity>> QueryAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Returns a page of entities with total-count metadata.</summary>
    Task<PagedResultData<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default);
}
