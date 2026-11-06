using Application.Common.Models;
using Application.DTOs.Address;
using Application.DTOs.Common;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Mapster;

namespace Application.Services;

// Expected failures are returned as Result errors; anything else is a bug and reaches the API's exception handler.
public sealed class AddressService(IAddressRepo repo, IUnitOfWork unitOfWork) : IAddressService
{
    public async Task<PagedResponse<AddressResponse>> GetPageAsync(PageRequest request)
    {
        var (addresses, totalCount) = await repo.GetPageAsync(request.Offset(), request.PageSize);
        return ToPage(addresses, request, totalCount);
    }

    public async Task<Result<AddressResponse>> GetByIdAsync(int id)
    {
        var address = await repo.GetByIdAsync(id);
        return address is null
            ? Result.Failure<AddressResponse>(AddressErrors.NotFound(id))
            : Result.Success(address.Adapt<AddressResponse>());
    }

    public async Task<PagedResponse<AddressResponse>> SearchAsync(string city, string? state, PageRequest request)
    {
        var (addresses, totalCount) = await repo.SearchByCityAndStateAsync(city, state, request.Offset(), request.PageSize);
        return ToPage(addresses, request, totalCount);
    }

    public async Task<Result<AddressResponse>> CreateAsync(CreateAddressRequest request)
    {
        var hash = Address.BuildHash(request.Street, request.City, request.State,
            request.ZipCode, request.Country);

        if (await repo.ExistsAsync(hash))
            return Result.Failure<AddressResponse>(AddressErrors.AlreadyExists);

        var address = request.Adapt<Address>();
        address.UniqueAddressHash = hash;
        repo.Add(address);

        // A simultaneous request can create the same address between the check above and this save.
        if (await unitOfWork.SaveAsync() == SaveOutcome.Duplicate)
            return Result.Failure<AddressResponse>(AddressErrors.AlreadyExists);

        return Result.Success(address.Adapt<AddressResponse>());
    }

    public async Task<Result> UpdateAsync(int id, UpdateAddressRequest request)
    {
        var address = await repo.GetByIdAsync(id);
        if (address is null)
            return Result.Failure(AddressErrors.NotFound(id));

        request.Adapt(address);

        var newHash = Address.BuildHash(address.Street, address.City, address.State,
            address.ZipCode, address.Country);

        if (await repo.ExistsAsync(id, newHash))
            return Result.Failure(AddressErrors.AlreadyExists);

        address.UniqueAddressHash = newHash;
        repo.Update(address);

        return await unitOfWork.SaveAsync() switch
        {
            SaveOutcome.Saved => Result.Success(),
            SaveOutcome.Conflict => Result.Failure(AddressErrors.ConcurrentChange),
            SaveOutcome.Duplicate => Result.Failure(AddressErrors.AlreadyExists),
            var outcome => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    public async Task DeleteAsync(int id)
    {
        var address = await repo.GetByIdAsync(id);
        if (address is null)
            return;

        repo.Remove(address);

        // A conflict here means another request already deleted it, which is the outcome asked for.
        await unitOfWork.SaveAsync();
    }

    private static PagedResponse<AddressResponse> ToPage(IReadOnlyList<Address> addresses, PageRequest request, int totalCount) =>
        new(addresses.Adapt<IReadOnlyList<AddressResponse>>(), request.Page, request.PageSize, totalCount);
}
