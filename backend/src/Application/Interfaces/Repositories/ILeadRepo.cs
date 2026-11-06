using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ILeadRepo : IGenericRepo<Lead>
{
    /// <summary>One page of leads, newest first, with their address and customer, and the total number of leads.</summary>
    Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPageAsync(int skip, int take);
}
