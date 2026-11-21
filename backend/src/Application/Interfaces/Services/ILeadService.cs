using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Lead;

namespace Application.Interfaces.Services;

/// <summary>Leads: the quote form's requests, and what the office does with them (FR-L1–L7, BR-L1–L3).</summary>
public interface ILeadService
{
    Task<PagedResponse<LeadSummaryResponse>> GetPageAsync(LeadPageRequest request);
    Task<Result<LeadResponse>> GetByIdAsync(int id);

    /// <summary>A new lead and its two notifications, or the open lead this email already has (BR-L1).</summary>
    Task<LeadCreated> CreateAsync(CreateLeadRequest request);

    /// <summary>Contacted, Disqualified or Lost, as BR-L2 allows; the time is now.</summary>
    Task<Result<LeadResponse>> UpdateStatusAsync(int id, UpdateLeadStatusRequest request);

    /// <summary>A Contacted lead becomes a customer and an installation, in one transaction (BR-L3).</summary>
    Task<Result<LeadResponse>> ConvertAsync(int id);

    /// <summary>An open lead's corrected name, email and phone (FR-L7).</summary>
    Task<Result<LeadResponse>> UpdateAsync(int id, UpdateLeadRequest request);
}
