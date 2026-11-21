using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AddressRepo(AppDbContext context) : GenericRepo<Address>(context), IAddressRepo
{
    // Never check, then insert: another request can insert between the two. The unique index decides instead; a
    // request that loses waits for the winner's commit, then reads its row.
    public async Task<Address> GetOrAddAsync(Address address)
    {
        await Context.Database.ExecuteSqlAsync($"""
            INSERT INTO addresses (street, city, state, zip_code, country, latitude, longitude, unique_address_hash)
            VALUES ({address.Street}, {address.City}, {address.State}, {address.ZipCode}, {address.Country},
                    {address.Latitude}, {address.Longitude}, {address.UniqueAddressHash})
            ON CONFLICT (unique_address_hash) DO NOTHING
            """);
        return await Context.Addresses.SingleAsync(a => a.UniqueAddressHash == address.UniqueAddressHash);
    }
}
