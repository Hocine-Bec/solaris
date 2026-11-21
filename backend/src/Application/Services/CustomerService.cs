using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Customers;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services;

public class CustomerService(ICustomerRepo customers) : ICustomerService
{
    public async Task<PagedResponse<CustomerSummaryResponse>> GetPageAsync(CustomerPageRequest request)
    {
        var (items, totalCount) = await customers.GetPageAsync(request.Q, request.Offset(), request.PageSize);
        return new PagedResponse<CustomerSummaryResponse>(
            items.Select(x => CustomerSummaryResponse.From(x.Customer, x.InstallationCount)).ToList(), request.Page,
            request.PageSize, totalCount);
    }

    public async Task<Result<CustomerResponse>> GetByIdAsync(int id)
    {
        var customer = await customers.GetWithInstallationsAsync(id);
        return customer is null
            ? Result.Failure<CustomerResponse>(CustomerErrors.NotFound(id))
            : Result.Success(CustomerResponse.From(customer));
    }
}
