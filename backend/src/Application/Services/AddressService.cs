using Application.Common.Models;
using Application.DTOs.Address;
using Application.DTOs.Common;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class AddressService(IAddressRepo repo, IUnitOfWork unitOfWork, ILogger<AddressService> log) : IAddressService
{
    public async Task<PagedResponse<AddressResponse>> GetPageAsync(PageRequest request)
    {
        var (addresses, totalCount) = await repo.GetPageAsync(request.Offset(), request.PageSize);
        return ToPage(addresses, request, totalCount);
    }

    public async Task<Result<AddressResponse>> GetByIdAsync(int id)
    {
        try
        {
            var address = await repo.GetByIdAsync(id);
            return address is null
                ? Result.NotFound<AddressResponse>($"Address with ID {id} not found.")
                : Result.Success(address.Adapt<AddressResponse>());
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(GetByIdAsync)} for {nameof(Address)} {id}");
            return Result.Failure<AddressResponse>(
                "An error occurred while retrieving the address.",
                ResultStatusCode.InternalServerError);
        }
    }

    public async Task<PagedResponse<AddressResponse>> SearchAsync(string city, string? state, PageRequest request)
    {
        var (addresses, totalCount) = await repo.SearchByCityAndStateAsync(city, state, request.Offset(), request.PageSize);
        return ToPage(addresses, request, totalCount);
    }

    public async Task<Result<AddressResponse>> CreateAsync(CreateAddressRequest request)
    {
        try
        {
            var hash = Address.BuildHash(request.Street, request.City, request.State,
                request.ZipCode, request.Country);

            if (await repo.ExistsAsync(hash))
                return Result.Failure<AddressResponse>("Address already exists.", ResultStatusCode.Conflict);
            
            var address = request.Adapt<Address>();
            address.UniqueAddressHash = hash;
            repo.Add(address);

            // A simultaneous request can create the same address between the check above and this save.
            if (await unitOfWork.SaveAsync() == SaveOutcome.Duplicate)
                return Result.Failure<AddressResponse>("Address already exists.", ResultStatusCode.Conflict);

            return Result.Success(address.Adapt<AddressResponse>(), ResultStatusCode.Created);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(CreateAsync)} for {nameof(Address)} New Entry");
            return Result.Failure<AddressResponse>("An error occurred while creating the address.",
                ResultStatusCode.InternalServerError);
        }
    }

    public async Task<Result> UpdateAsync(int id, UpdateAddressRequest request)
    {
        try
        {
            var address = await repo.GetByIdAsync(id);
            if (address is null) 
                return Result.NotFound($"Address with ID {id} not found.");

            request.Adapt(address);
            
            var newHash = Address.BuildHash(address.Street, address.City, address.State, 
                address.ZipCode, address.Country);
            
            if (await repo.ExistsAsync(id, newHash))
                return Result.Failure<AddressResponse>("Address already exists.", ResultStatusCode.Conflict);

            address.UniqueAddressHash = newHash;
            repo.Update(address);

            var outcome = await unitOfWork.SaveAsync();
            if (outcome == SaveOutcome.Conflict)
                log.LogError($"Conflict while {nameof(UpdateAsync)} for {nameof(Address)} {id}");

            return outcome switch
            {
                SaveOutcome.Saved => Result.Success(ResultStatusCode.NoContent),
                SaveOutcome.Conflict => Result.Failure("The address was modified by another user.", ResultStatusCode.Conflict),
                SaveOutcome.Duplicate => Result.Failure("Address already exists.", ResultStatusCode.Conflict),
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
            };
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(UpdateAsync)} for {nameof(Address)} {id}");
            return Result.Failure("An error occurred while updating the address.", ResultStatusCode.InternalServerError);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var address = await repo.GetByIdAsync(id);
            if (address is null) return Result.Success(ResultStatusCode.NoContent);

            repo.Remove(address);

            // A conflict here means another request already deleted it, which is the outcome asked for.
            await unitOfWork.SaveAsync();
            return Result.Success(ResultStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(DeleteAsync)} for {nameof(Address)} {id}");
            return Result.Failure("An error occurred while deleting the address.", ResultStatusCode.InternalServerError);
        }
    }

    private static PagedResponse<AddressResponse> ToPage(IReadOnlyList<Address> addresses, PageRequest request, int totalCount) =>
        new(addresses.Adapt<IReadOnlyList<AddressResponse>>(), request.Page, request.PageSize, totalCount);
}
