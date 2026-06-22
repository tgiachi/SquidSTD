using System.Linq.Expressions;
using FreeSql;
using Serilog;
using SquidStd.Database.Abstractions.Data;
using SquidStd.Database.Abstractions.Data.Entities;
using SquidStd.Database.Abstractions.Interfaces.Data;
using SquidStd.Database.Interfaces.Services;

namespace SquidStd.Database.Data;

/// <summary>
/// FreeSql-backed <see cref="IDataAccess{TEntity}"/>. Writes run inside a unit of work with rollback.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class FreeSqlDataAccess<TEntity> : IDataAccess<TEntity>
    where TEntity : BaseEntity
{
    private static readonly ILogger Logger = Log.ForContext<FreeSqlDataAccess<TEntity>>();

    private readonly IFreeSql _orm;

    /// <summary>
    /// Initializes the data access over the shared FreeSql instance.
    /// </summary>
    /// <param name="databaseService">The database service that owns the FreeSql instance.</param>
    public FreeSqlDataAccess(IDatabaseService databaseService)
    {
        _orm = databaseService.Orm;
    }

    /// <inheritdoc />
    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.CreateVersion7();
        }

        entity.Created = now;
        entity.Updated = now;

        await RunInTransactionAsync(
            transaction => _orm.Insert(entity).WithTransaction(transaction).ExecuteAffrowsAsync(cancellationToken),
            "Insert",
            1,
            cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _orm.Select<TEntity>().Where(e => e.Id == id).FirstAsync(cancellationToken)!;

    /// <inheritdoc />
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Updated = DateTimeOffset.UtcNow;

        await RunInTransactionAsync(
            transaction => _orm.Update<TEntity>().SetSource(entity).WithTransaction(transaction).ExecuteAffrowsAsync(cancellationToken),
            "Update",
            1,
            cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var affected = await RunInTransactionAsync(
            transaction => _orm.Delete<TEntity>().Where(e => e.Id == id).WithTransaction(transaction).ExecuteAffrowsAsync(cancellationToken),
            "Delete",
            null,
            cancellationToken);

        return affected > 0;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        => DeleteAsync(entity.Id, cancellationToken);

    /// <inheritdoc />
    public Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var select = _orm.Select<TEntity>();

        if (predicate is not null)
        {
            select = select.Where(predicate);
        }

        return select.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => _orm.Select<TEntity>().Where(predicate).AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var list = Materialize(entities, stampUpdated: true);

        return RunInTransactionAsync(
            transaction => _orm.Insert(list).WithTransaction(transaction).ExecuteAffrowsAsync(cancellationToken),
            "BulkInsert",
            list.Count,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        var now = DateTimeOffset.UtcNow;

        foreach (var entity in list)
        {
            entity.Updated = now;
        }

        return RunInTransactionAsync(
            transaction => _orm.Update<TEntity>().SetSource(list).WithTransaction(transaction).ExecuteAffrowsAsync(cancellationToken),
            "BulkUpdate",
            list.Count,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => RunInTransactionAsync(
            transaction => _orm.Delete<TEntity>().Where(predicate).WithTransaction(transaction).ExecuteAffrowsAsync(cancellationToken),
            "BulkDelete",
            null,
            cancellationToken);

    /// <inheritdoc />
    public ISelect<TEntity> Query()
        => _orm.Select<TEntity>();

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> QueryAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var select = _orm.Select<TEntity>();

        if (predicate is not null)
        {
            select = select.Where(predicate);
        }

        return await select.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResultData<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var select = _orm.Select<TEntity>();

        if (predicate is not null)
        {
            select = select.Where(predicate);
        }

        var total = await select.CountAsync(cancellationToken);

        if (orderBy is not null)
        {
            select = descending ? select.OrderByDescending(orderBy) : select.OrderBy(orderBy);
        }

        var items = await select.Page(page, pageSize).ToListAsync(cancellationToken);

        return PagedResultData<TEntity>.Create(items, page, pageSize, total);
    }

    private void EnsureSynced()
    {
        // Create/alter the table outside any transaction (idempotent, cached by FreeSql) so that
        // SQLite DDL never collides with an open unit-of-work file lock.
        if (_orm.CodeFirst.IsAutoSyncStructure)
        {
            _orm.CodeFirst.SyncStructure<TEntity>();
        }
    }

    private static List<TEntity> Materialize(IEnumerable<TEntity> entities, bool stampUpdated)
    {
        var now = DateTimeOffset.UtcNow;
        var list = entities.ToList();

        foreach (var entity in list)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.CreateVersion7();
            }

            entity.Created = now;

            if (stampUpdated)
            {
                entity.Updated = now;
            }
        }

        return list;
    }

    private async Task<int> RunInTransactionAsync(
        Func<System.Data.Common.DbTransaction, Task<int>> action,
        string operation,
        int? expectedCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureSynced();

        using var uow = _orm.CreateUnitOfWork();

        try
        {
            var transaction = uow.GetOrBeginTransaction();
            Logger.Verbose("{Operation} starting (expected {Expected})", operation, expectedCount);

            var affected = await action(transaction);

            uow.Commit();
            Logger.Verbose("{Operation} committed ({Affected} rows)", operation, affected);

            return affected;
        }
        catch (Exception ex)
        {
            uow.Rollback();
            Logger.Error(ex, "{Operation} rolled back", operation);
            throw;
        }
    }
}
