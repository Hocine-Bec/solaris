using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Lead;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepo _repo;
    private readonly IAddressRepo _addressRepo;
    private readonly ICustomerRepo _customerRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueueHandler _emailQueueHandler;
    private readonly ILogger<LeadService> _log;

    public LeadService(ILeadRepo repo, IAddressRepo addressRepo, ICustomerRepo customerRepo, IUnitOfWork unitOfWork,
        IEmailQueueHandler emailQueueHandler, ILogger<LeadService> log)
    {
        _repo = repo;
        _addressRepo = addressRepo;
        _customerRepo = customerRepo;
        _unitOfWork = unitOfWork;
        _emailQueueHandler = emailQueueHandler;
        _log = log;
    }

    public async Task<PagedResponse<LeadResponse>> GetPageAsync(PageRequest request)
    {
        var (leads, totalCount) = await _repo.GetPageAsync(request.Offset(), request.PageSize);
        return new PagedResponse<LeadResponse>(leads.Adapt<IReadOnlyList<LeadResponse>>(), request.Page,
            request.PageSize, totalCount);
    }

    public async Task<Result<LeadResponse>> GetByIdAsync(int id)
    {
        try
        {
            var lead = await _repo.GetByIdAsync(id);
            if (lead is null)
                return Result.NotFound<LeadResponse>($"Lead with ID {id} not found.");

            var response = lead.Adapt<LeadResponse>();
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(GetByIdAsync)} for {nameof(Lead)} {id}");
            return Result.Failure<LeadResponse>(
                "An error occurred while retrieving the lead.",
                ResultStatusCode.InternalServerError);
        }
    }

    public async Task<Result<LeadResponse>> CreateAsync(CreateLeadRequest request)
    {
        try
        {
            var lead = request.Adapt<Lead>();
            lead.Status = LeadStatus.New;
            lead.CreatedAt = DateTime.UtcNow;

            var address = lead.Address;
            address.UniqueAddressHash = Address.BuildHash(address.Street, address.City, address.State,
                address.ZipCode, address.Country);
            if (await _addressRepo.ExistsAsync(address.UniqueAddressHash))
                return Result.Failure<LeadResponse>("Failed to create the address.");

            // The lead and its address are saved together, in one transaction.
            _repo.Add(lead);
            if (await _unitOfWork.SaveAsync() == SaveOutcome.Duplicate)
                return Result.Failure<LeadResponse>("Failed to create the address.");

            var fullAddress = $"{address.Street}, {address.State}, {address.ZipCode}, {address.City}, {address.Country}";
            var fullName = $"{lead.FirstName} {lead.LastName}";

            // Enqueue background job
            _emailQueueHandler.EnqueueLeadEmails(lead.Id, lead.Email, lead.FirstName,
                fullName, lead.PhoneNumber, fullAddress);

            var response = lead.Adapt<LeadResponse>();
            return Result.Success(response, ResultStatusCode.Created);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(CreateAsync)} for {nameof(Lead)}");
            return Result.Failure<LeadResponse>(
                "An error occurred while creating the lead.",
                ResultStatusCode.InternalServerError);
        }
    }

    public async Task<Result> UpdateLeadStatusAsync(int id, UpdateLeadStatusRequest request)
    {
        try
        {
            var lead = await _repo.GetByIdAsync(id);
            if (lead is null)
                return Result.NotFound($"Lead with ID {id} not found.");

            lead.Status = Enum.Parse<LeadStatus>(request.Status);
            if(lead.Status == LeadStatus.Converted)
                lead.ConvertedAt = DateTime.UtcNow;

            lead.ContactedAt = request.ContactedAt;
            lead.Notes = request.Notes;

            _repo.Update(lead);
            return await _unitOfWork.SaveAsync() == SaveOutcome.Saved
                ? Result.Success(ResultStatusCode.NoContent)
                : Result.Failure("The lead was changed by another user.", ResultStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(UpdateLeadStatusAsync)} for {nameof(Lead)} {id}");
            return Result.Failure("An error occurred while updating the lead.", ResultStatusCode.InternalServerError);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var lead = await _repo.GetByIdAsync(id);
            if (lead is null)
                return Result.Success(ResultStatusCode.NoContent);

            _repo.Remove(lead);

            // A conflict here means another request already deleted it, which is the outcome asked for.
            await _unitOfWork.SaveAsync();
            return Result.Success(ResultStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(DeleteAsync)} for {nameof(Lead)} {id}");
            return Result.Failure("An error occurred while deleting the lead.", ResultStatusCode.InternalServerError);
        }
    }

    public async Task<Result> ConvertLeadToCustomerAsync(ConvertLeadRequest request)
    {
        try
        {
            var lead = await _repo.GetByIdAsync(request.LeadId);
            if (lead is null)
                return Result.NotFound($"Lead with ID {request.LeadId} not found.");

            if (lead.Status == LeadStatus.Converted)
                return Result.Failure("Lead has already been converted.");

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
                SaveOutcome.Saved => Result.Success("Lead converted to customer successfully."),
                SaveOutcome.Duplicate => Result.Failure("A customer with this email already exists.", ResultStatusCode.Conflict),
                _ => Result.Failure("The lead was changed by another user.", ResultStatusCode.Conflict)
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(ConvertLeadToCustomerAsync)} for {nameof(Lead)} {request.LeadId}");
            return Result.Failure("An error occurred while converting the lead.", ResultStatusCode.InternalServerError);
        }
    }
}
