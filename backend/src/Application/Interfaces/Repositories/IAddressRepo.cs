using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>Addresses come with their lead: the lead's service saves them, and only asks whether one exists.</summary>
public interface IAddressRepo : IGenericRepo<Address>
{
    /// <summary>Whether an address with this key exists (ADR 5).</summary>
    Task<bool> ExistsAsync(string uniqueAddressHash);
}
