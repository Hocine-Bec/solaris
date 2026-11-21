using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IInstallationRepo : IGenericRepo<Installation>
{
    /// <summary>The ID of the installation the lead's conversion created; null if it has none.</summary>
    Task<int?> GetIdForLeadAsync(int leadId);
}
