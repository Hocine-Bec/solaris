using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Lead;

namespace Application.Interfaces.Services;

public interface ILeadService
{
    Task<PagedResponse<LeadResponse>> GetPageAsync(PageRequest request);
    Task<Result<LeadResponse>> GetByIdAsync(int id);
    Task<Result<LeadResponse>> CreateAsync(CreateLeadRequest request);
    Task<Result> UpdateLeadStatusAsync(int id, UpdateLeadStatusRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> ConvertLeadToCustomerAsync(ConvertLeadRequest request);
}
