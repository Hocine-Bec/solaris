using Application.Common.Models;
using Application.DTOs.Address;
using Application.DTOs.Common;

namespace Application.Interfaces.Services;

public interface IAddressService
{
    Task<PagedResponse<AddressResponse>> GetPageAsync(PageRequest request);
    Task<Result<AddressResponse>> GetByIdAsync(int id);
    Task<PagedResponse<AddressResponse>> SearchAsync(string city, string? state, PageRequest request);
    Task<Result<AddressResponse>> CreateAsync(CreateAddressRequest request);
    Task<Result> UpdateAsync(int id, UpdateAddressRequest request);
    Task DeleteAsync(int id);
}
