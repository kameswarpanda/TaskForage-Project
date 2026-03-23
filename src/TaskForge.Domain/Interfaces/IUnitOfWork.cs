namespace TaskForge.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern for managing transactions across repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
