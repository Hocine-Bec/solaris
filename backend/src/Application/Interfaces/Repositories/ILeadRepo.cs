using Application.Interfaces.Repositories.Base;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

/// <summary>Leads. Reading one by ID tracks it, with its address and customer, so a save writes only what changed.</summary>
public interface ILeadRepo : IGenericRepo<Lead>
{
    /// <summary>
    /// One page of the leads in these statuses (every lead when none is given), newest first, with their address, and
    /// how many leads match.
    /// </summary>
    Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPageAsync(IReadOnlyCollection<LeadStatus> statuses, int skip,
        int take);

    /// <summary>
    /// The ID of an open lead (New or Contacted) with this email, compared as <c>Emails.Normalize</c> does, other than
    /// <paramref name="exceptId"/>; null if there is none (BR-L1).
    /// </summary>
    Task<int?> FindOpenIdByEmailAsync(string email, int? exceptId = null);
}
