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

    public virtual async Task<int> CreateAsync(T entity)
    {
        await _set.AddAsync(entity);
        return await Context.SaveChangesAsync();
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        _set.Update(entity);
        return await Context.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> DeleteAsync(T entity)
    {
        _set.Remove(entity);
        return await Context.SaveChangesAsync() > 0;
    }
}