using Application.Interfaces.Repositories.Base;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Base;

public class GenericRepo<T>(AppDbContext context) : IGenericRepo<T>
    where T : class
{
    protected readonly AppDbContext Context = context;
    private readonly DbSet<T> _set = context.Set<T>();

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _set.AsNoTracking().ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _set.FindAsync(id);
    }

    public virtual void Add(T entity) => _set.Add(entity);

    public virtual void Update(T entity) => _set.Update(entity);

    public virtual void Remove(T entity) => _set.Remove(entity);
}