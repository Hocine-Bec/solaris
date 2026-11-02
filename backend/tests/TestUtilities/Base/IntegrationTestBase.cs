using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TestUtilities.Base;

public abstract class IntegrationTestBase : IDisposable
{
    protected readonly AppDbContext Context;

    protected IntegrationTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        Context = new AppDbContext(options);
    }

    protected async Task SeedAsync<T>(params T[] entities)
    {
        foreach (var entity in entities)
        {
            if(entity == null) continue;
            await Context.AddAsync(entity);
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear(); 
    }

    protected void ClearTracking()
    {
        Context.ChangeTracker.Clear();
    }
    
    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
