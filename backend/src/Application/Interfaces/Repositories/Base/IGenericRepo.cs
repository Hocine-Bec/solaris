namespace Application.Interfaces.Repositories.Base;

/// <summary>
/// Reads and changes one kind of entity. Changes are only saved when the service calls
/// <c>IUnitOfWork.SaveAsync</c>, once per operation.
/// </summary>
public interface IGenericRepo<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
