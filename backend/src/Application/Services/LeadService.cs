using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Lead;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Services;

// Expected failures are returned as Result errors; anything else is a bug and reaches the API's exception handler.
public class LeadService : ILeadService
{
    private readonly ILeadRepo _repo;
    private readonly IAddressRepo _addressRepo;
    private readonly ICustomerRepo _customerRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueueHandler _emailQueueHandler;

    public LeadService(ILeadRepo repo, IAddressRepo addressRepo, ICustomerRepo customerRepo, IUnitOfWork unitOfWork,
        IEmailQueueHandler emailQueueHandler)
    {
        _repo = repo;
        _addressRepo = addressRepo;
        _customerRepo = customerRepo;
        _unitOfWork = unitOfWork;
        _emailQueueHandler = emailQueueHandler;
    }

    public async Task<PagedResponse<LeadResponse>> GetPageAsync(PageRequest request)
    {
        var (leads, totalCount) = await _repo.GetPageAsync(request.Offset(), request.PageSize);
        return new PagedResponse<LeadResponse>(leads.Adapt<IReadOnlyList<LeadResponse>>(), request.Page,
            request.PageSize, totalCount);
    }

    public async Task<Result<LeadResponse>> GetByIdAsync(int id)
    {
        var lead = await _repo.GetByIdAsync(id);
        return lead is null
            ? Result.Failure<LeadResponse>(LeadErrors.NotFound(id))
            : Result.Success(lead.Adapt<LeadResponse>());
    }

    public async Task<Result<LeadResponse>> CreateAsync(CreateLeadRequest request)
    {
        var lead = request.Adapt<Lead>();
        lead.Status = LeadStatus.New;
        lead.CreatedAt = DateTime.UtcNow;

        var address = lead.Address;
        address.UniqueAddressHash = Address.BuildHash(address.Street, address.City, address.State,
            address.ZipCode, address.Country);
        if (await _addressRepo.ExistsAsync(address.UniqueAddressHash))
            return Result.Failure<LeadResponse>(AddressErrors.AlreadyExists);

        // The lead and its address are saved together, in one transaction.
        _repo.Add(lead);
        if (await _unitOfWork.SaveAsync() == SaveOutcome.Duplicate)
            return Result.Failure<LeadResponse>(AddressErrors.AlreadyExists);

        var fullAddress = $"{address.Street}, {address.State}, {address.ZipCode}, {address.City}, {address.Country}";
        var fullName = $"{lead.FirstName} {lead.LastName}";

        // Enqueue background job
        _emailQueueHandler.EnqueueLeadEmails(lead.Id, lead.Email, lead.FirstName,
            fullName, lead.PhoneNumber, fullAddress);

        return Result.Success(lead.Adapt<LeadResponse>());
    }

    public async Task<Result> UpdateLeadStatusAsync(int id, UpdateLeadStatusRequest request)
    {
        var lead = await _repo.GetByIdAsync(id);
        if (lead is null)
            return Result.Failure(LeadErrors.NotFound(id));

        lead.Status = Enum.Parse<LeadStatus>(request.Status);
        if (lead.Status == LeadStatus.Converted)
            lead.ConvertedAt = DateTime.UtcNow;

        lead.ContactedAt = request.ContactedAt;
        lead.Notes = request.Notes;

        _repo.Update(lead);
        return await _unitOfWork.SaveAsync() == SaveOutcome.Saved
            ? Result.Success()
            : Result.Failure(LeadErrors.ConcurrentChange);
    }

    public async Task DeleteAsync(int id)
    {
        var lead = await _repo.GetByIdAsync(id);
        if (lead is null)
            return;

        _repo.Remove(lead);

        // A conflict here means another request already deleted it, which is the outcome asked for.
        await _unitOfWork.SaveAsync();
    }

    public async Task<Result> ConvertLeadToCustomerAsync(ConvertLeadRequest request)
    {
        var lead = await _repo.GetByIdAsync(request.LeadId);
        if (lead is null)
            return Result.Failure(LeadErrors.NotFound(request.LeadId));

        if (lead.Status == LeadStatus.Converted)
            return Result.Failure(LeadErrors.AlreadyConverted(request.LeadId));

        // The new customer and the converted lead are saved together; the lead gets the customer's real ID.
        var customer = lead.Adapt<Customer>();
        _customerRepo.Add(customer);

        lead.Status = LeadStatus.Converted;
        lead.ConvertedAt = DateTime.UtcNow;
        lead.Customer = customer;
        lead.Notes = request.AdditionalNotes;
        _repo.Update(lead);

        return await _unitOfWork.SaveAsync() switch
        {
            SaveOutcome.Saved => Result.Success(),
            SaveOutcome.Duplicate => Result.Failure(CustomerErrors.EmailTaken),
            _ => Result.Failure(LeadErrors.ConcurrentChange)
        };
    }
}
