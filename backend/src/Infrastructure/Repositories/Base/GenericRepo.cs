using Application.Interfaces.Repositories.Base;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Base;

public class GenericRepo<T>(AppDbContext context) : IGenericRepo<T>
    where T : class
{
    protected readonly AppDbContext Context = context;
    private readonly DbSet<T> _set = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _set.FindAsync(id);
    }

    public virtual void Add(T entity) => _set.Add(entity);

    public virtual void Update(T entity) => _set.Update(entity);

    public virtual void Remove(T entity) => _set.Remove(entity);

    /// <summary>
    /// One page of an ordered query, and the number of rows in the whole query. The order must be total (end with a
    /// unique column), or rows could move between pages.
    /// </summary>
    protected static async Task<(IReadOnlyList<T> Items, int TotalCount)> ToPageAsync(IOrderedQueryable<T> query,
        int skip, int take)
    {
        var totalCount = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, totalCount);
    }
}
