namespace TeamOrganization.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> FindFirstByConditionAsync(
        Func<IQueryable<T>, IQueryable<T>> condition,
        bool trackChanges = false
    );

    Task<IReadOnlyList<T>> FindAllAsync(bool trackChanges = false);

    Task<IReadOnlyList<T>> FindAllByConditionAsync(
        Func<IQueryable<T>, IQueryable<T>> condition,
        bool trackChanges = false
    );

    IQueryable<T> QueryByCondition(
        Func<IQueryable<T>, IQueryable<T>> condition,
        bool trackChanges = false
    );

    T Add(T entity);
    IReadOnlyList<T> AddRange(IReadOnlyList<T> entities);
}