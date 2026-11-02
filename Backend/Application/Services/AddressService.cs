using Application.Common.Exceptions;
using Application.Common.Models;
using Application.DTOs.Address;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class AddressService(IAddressRepo repo, ILogger<AddressService> log) : IAddressService
{
    public async Task<Result<IReadOnlyList<AddressResponse>>> GetListAsync()
    {
        try
        {
            var addresses = await repo.GetAllAsync();
            var response = addresses.Adapt<IReadOnlyList<AddressResponse>>();
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(GetByIdAsync)} for {nameof(Address)} List");
            return Result.Failure<IReadOnlyList<AddressResponse>>(
                "An error occurred while retrieving addresses.",
                ResultStatusCode.InternalServerError);
        }
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
    
    public async Task<Result<IEnumerable<AddressResponse>>> SearchAsync(string city, string? state = null)
    {
        try
        {
            var response = await repo.SearchByCityAndStateAsync(city, state);
            return Result.Success(response.Adapt<IEnumerable<AddressResponse>>());
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(SearchAsync)} for {nameof(Address)} city: {city} state: {state ?? string.Empty}");
            return Result.Failure<IEnumerable<AddressResponse>>(
                "An error occurred while searching for the address.",
                ResultStatusCode.InternalServerError);
        }
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
            await repo.CreateAsync(address);
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
            var updated = await repo.UpdateAsync(address);
            return updated
                ? Result.Success(ResultStatusCode.NoContent)
                : Result.Failure("No changes were applied.", ResultStatusCode.BadRequest);
        }
        catch (ConcurrencyConflictException ex)
        {
            log.LogError(ex, $"Error while {nameof(UpdateAsync)} for {nameof(Address)} {id}");
            return Result.Failure("The address was modified by another user.", ResultStatusCode.Conflict);
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

            var deleted = await repo.DeleteAsync(address);
            return deleted
                ? Result.Success(ResultStatusCode.NoContent)
                : Result.Failure("Failed to delete the address.", ResultStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"Error while {nameof(DeleteAsync)} for {nameof(Address)} {id}");
            return Result.Failure("An error occurred while deleting the address.", ResultStatusCode.InternalServerError);
        }
    }
}