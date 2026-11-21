using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Customers;

namespace Application.Interfaces.Services;

/// <summary>Customers, read by the office (FR-C1); conversions create them (BR-L3).</summary>
public interface ICustomerService
{
    Task<PagedResponse<CustomerSummaryResponse>> GetPageAsync(CustomerPageRequest request);
    Task<Result<CustomerResponse>> GetByIdAsync(int id);
}
