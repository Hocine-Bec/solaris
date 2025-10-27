using Application.Common.Models;
using Application.DTOs.Address;
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
    private readonly IEmailService _emailService;
    private readonly ICustomerRepo _customerRepo;
    private readonly IAddressService _addressRepo;
    private readonly ILogger<AddressService> _log;
    
    public LeadService(ILeadRepo repo, IEmailService emailService, ICustomerRepo customerRepo, IAddressService addressRepo, ILogger<AddressService> log)
    {
        _emailService = emailService;
        _customerRepo = customerRepo;
        _addressRepo = addressRepo;
        _log = log;
        _repo = repo;
    }
    
    public async Task<Result<IReadOnlyList<LeadResponse>>> GetListAsync()
    {
        try
        {
            var leads = await _repo.GetAllAsync();
            var response = leads.Adapt<IReadOnlyList<LeadResponse>>();
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(GetByIdAsync)} for {nameof(Lead)}s List");
            return Result.Failure<IReadOnlyList<LeadResponse>>(
                "An error occurred while retrieving leads.",
                ResultStatusCode.InternalServerError);
        }
    }
    public async Task<Result<LeadResponse>> GetByIdAsync(int id)
    {
        try
        {
            var lead = await _repo.GetByIdAsync(id);
            if (lead is null)
                Result.NotFound<LeadResponse>($"Lead with ID {id} not found.");

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

            var address = lead.Address.Adapt<CreateAddressRequest>();
            var addressResult = await _addressRepo.CreateAsync(address);
            if (!addressResult.IsSuccess)
                return Result.Failure<LeadResponse>("Failed to create the address.");

            lead.AddressId = addressResult.Value.Id;
            lead.Address = null!;
            var created = await _repo.CreateAsync(lead);
            if (created <= 0)
                return Result.Failure<LeadResponse>("Failed to create the lead.");
            
            var fullAddress = $"{addressResult.Value.Street}, {addressResult.Value.State}, {addressResult.Value.ZipCode}, {addressResult.Value.City}, {addressResult.Value.Country}";
            
            // Send notification email
            await _emailService.SendLeadEmailAsync(lead.Email, lead.FirstName, fullAddress);
            await _emailService.SendSalesNotificationEmailAsync(
                created, 
                $"{lead.FirstName} {lead.LastName}", 
                lead.Email, 
                lead.PhoneNumber, 
                fullAddress);
            
            var response = created.Adapt<LeadResponse>();
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
            
            var updated = await _repo.UpdateAsync(lead);  
            return updated ? Result.Success(ResultStatusCode.NoContent)
                : Result.Failure("No changes were applied.", ResultStatusCode.BadRequest);
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

            var deleted = await _repo.DeleteAsync(lead);
            return deleted
                ? Result.Success(ResultStatusCode.NoContent)
                : Result.Failure("Failed to delete the lead.");
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

            var customer = lead.Adapt<Customer>();
            var createdCustomer = await _customerRepo.CreateAsync(customer);
            if (createdCustomer <= 0)
                return Result.Failure("Failed to create the customer.");

            lead.Status = LeadStatus.Converted;
            lead.ConvertedAt = DateTime.UtcNow;
            lead.CustomerId = createdCustomer;
            lead.Notes = request.AdditionalNotes;

            await _repo.UpdateAsync(lead);
            return Result.Success("Lead converted to customer successfully.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Error while {nameof(ConvertLeadToCustomerAsync)} for {nameof(Lead)} {request.LeadId}");
            return Result.Failure("An error occurred while converting the lead.", ResultStatusCode.InternalServerError);
        }
    }
}