using Application.Common.Models;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Data;

/// <summary>Saves the context's changes once and turns the failures a service can expect into a <see cref="SaveOutcome"/>.</summary>
public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task<SaveOutcome> SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return SaveOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SaveOutcome.Conflict;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return SaveOutcome.Duplicate;
        }
    }
}
