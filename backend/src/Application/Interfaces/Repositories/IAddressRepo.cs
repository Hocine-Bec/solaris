using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IAddressRepo : IGenericRepo<Address>
{
    /// <summary>One page of addresses, by ID, and the total number of addresses.</summary>
    Task<(IReadOnlyList<Address> Items, int TotalCount)> GetPageAsync(int skip, int take);

    /// <summary>One page of the addresses whose city contains <paramref name="city"/> (and in <paramref name="state"/>, if given), by city then street.</summary>
    Task<(IReadOnlyList<Address> Items, int TotalCount)> SearchByCityAndStateAsync(string city, string? state,
        int skip, int take);

    Task<bool> ExistsAsync(string uniqueAddressHash);
    Task<bool> ExistsAsync(int id, string uniqueAddressHash);
}
