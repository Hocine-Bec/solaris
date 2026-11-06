using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AddressRepo(AppDbContext context) : GenericRepo<Address>(context), IAddressRepo
{
    public async Task<(IReadOnlyList<Address> Items, int TotalCount)> GetPageAsync(int skip, int take)
    {
        return await ToPageAsync(Context.Addresses.AsNoTracking().OrderBy(a => a.Id), skip, take);
    }

    public async Task<(IReadOnlyList<Address> Items, int TotalCount)> SearchByCityAndStateAsync(string city,
        string? state, int skip, int take)
    {
        var query = Context.Addresses.AsNoTracking();

        query = query.Where(a => a.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(a => a.State.ToLower() == state.ToLower());

        // The ID last, so addresses with the same city and street keep their place between pages.
        return await ToPageAsync(query.OrderBy(a => a.City).ThenBy(a => a.Street).ThenBy(a => a.Id), skip, take);
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