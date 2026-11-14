using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AddressRepo(AppDbContext context) : GenericRepo<Address>(context), IAddressRepo
{
    public async Task<bool> ExistsAsync(string uniqueAddressHash)
    {
        return await Context.Addresses
            .AsNoTracking()
            .AnyAsync(a => a.UniqueAddressHash == uniqueAddressHash);
    }
}
