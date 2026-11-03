using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AddressRepo(AppDbContext context) : GenericRepo<Address>(context), IAddressRepo
{
    public async Task<IReadOnlyList<Address>> SearchByCityAndStateAsync(string city, string? state = null)
    {
        var query = Context.Addresses.AsNoTracking();

        query = query.Where(a => a.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(a => a.State.ToLower() == state.ToLower());

        return await query
            .OrderBy(a => a.City)
            .ThenBy(a => a.Street)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string uniqueAddressHash)
    {
        return await Context.Addresses
            .AsNoTracking()
            .AnyAsync(a => a.UniqueAddressHash == uniqueAddressHash);
    }

    public async Task<bool> ExistsAsync(int id, string uniqueAddressHash)
    {
        return await Context.Addresses
            .AsNoTracking()
            .AnyAsync(a => a.UniqueAddressHash == uniqueAddressHash && a.Id != id);
    }
}