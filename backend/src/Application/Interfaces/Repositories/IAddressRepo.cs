using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>Addresses come with their lead: the lead's service stores each one once, by its key (ADR 5).</summary>
public interface IAddressRepo : IGenericRepo<Address>
{
    /// <summary>
    /// The stored address with this address's key, inserted first if there is none, and tracked. The insert can't be
    /// duplicated (<c>ON CONFLICT DO NOTHING</c>, then a read), so two requests for one property at the same moment
    /// share one row. It runs at once: call it inside the operation's transaction.
    /// </summary>
    Task<Address> GetOrAddAsync(Address address);
}
