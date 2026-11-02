using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IAddressRepo : IGenericRepo<Address>
{
    Task<IReadOnlyList<Address>> SearchByCityAndStateAsync(string city, string? state = null);
    Task<bool> ExistsAsync(string uniqueAddressHash);
    Task<bool> ExistsAsync(int id, string uniqueAddressHash);
}