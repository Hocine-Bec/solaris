using System.Diagnostics;
using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Lead;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Services;

// Expected failures are returned as Result errors; anything else is a bug and reaches the API's exception handler.
// A lead read by ID is tracked, so saving writes only the columns a move or a correction changed.
public class LeadService(
    ILeadRepo leads,
    IAddressRepo addresses,
    ICustomerRepo customers,
    IInstallationRepo installations,
    IUnitOfWork unitOfWork,
    ILeadNotifier notifier,
    TimeProvider time) : ILeadService
{
    public async Task<PagedResponse<LeadSummaryResponse>> GetPageAsync(LeadPageRequest request)
    {
        var statuses = request.Status.Select(Enum.Parse<LeadStatus>).ToHashSet();
        var (items, totalCount) = await leads.GetPageAsync(statuses, request.Offset(), request.PageSize);
        return new PagedResponse<LeadSummaryResponse>(items.Select(LeadSummaryResponse.From).ToList(), request.Page,
            request.PageSize, totalCount);
    }

    public async Task<Result<LeadResponse>> GetByIdAsync(int id)
    {
        var lead = await leads.GetByIdAsync(id);
        return lead is null
            ? Result.Failure<LeadResponse>(LeadErrors.NotFound(id))
            : Result.Success(await DetailsAsync(lead));
    }

    public async Task<LeadCreated> CreateAsync(CreateLeadRequest request)
    {
        // BR-L1, checked here and not by the database, on purpose: this is the naive version P3a measures, which lets
        // duplicates through when the same form is sent many times at once. The fix is a unique index.
        if (await leads.FindOpenIdByEmailAsync(request.Email) is { } openId)
            return new LeadCreated(openId, IsNew: false);

        var lead = request.Adapt<Lead>();
        lead.Status = LeadStatus.New;
        lead.CreatedAt = Now();
        var address = lead.Address;
        address.UniqueAddressHash = Address.BuildHash(address.Street, address.City, address.State, address.ZipCode,
            address.Country);

        // The property (stored once, by its key), the lead and its two notifications commit together, or not at all
        // (BR-N1).
        await using (var transaction = await unitOfWork.BeginTransactionAsync())
        {
            lead.Address = await addresses.GetOrAddAsync(address);
            leads.Add(lead);
            notifier.OnSaving(lead);
            var outcome = await unitOfWork.SaveAsync();
            if (outcome != SaveOutcome.Saved)
                throw new UnreachableException($"Adding a lead can't end as {outcome}: nothing it writes is unique.");
            await transaction.CommitAsync();
        }

        await notifier.OnSavedAsync(lead);
        return new LeadCreated(lead.Id, IsNew: true);
    }

    public async Task<Result<LeadResponse>> UpdateStatusAsync(int id, UpdateLeadStatusRequest request)
    {
        var lead = await leads.GetByIdAsync(id);
        if (lead is null)
            return Result.Failure<LeadResponse>(LeadErrors.NotFound(id));

        var status = Enum.Parse<LeadStatus>(request.Status);
        if (!lead.TryMoveTo(status, Now()))
            return Result.Failure<LeadResponse>(LeadErrors.TransitionNotAllowed(lead.Status, status));

        return await SaveAsync(lead);
    }

    public async Task<Result<LeadResponse>> ConvertAsync(int id, int actingUserId)
    {
        // The customer (reused when one has this email), the installation and the lead commit together, or not at all.
        await using var transaction = await unitOfWork.BeginTransactionAsync();
        var lead = await leads.GetByIdAsync(id);
        if (lead is null)
            return Result.Failure<LeadResponse>(LeadErrors.NotFound(id));

        var now = Now();
        if (!lead.TryMoveTo(LeadStatus.Converted, now))
            return Result.Failure<LeadResponse>(LeadErrors.TransitionNotAllowed(lead.Status, LeadStatus.Converted));

        var customer = await customers.GetOrAddAsync(Customer.FromLead(lead, now));
        lead.Customer = customer;
        var installation = Installation.ForConvertedLead(lead, customer.Id,
            UsTimeZones.ProposedFor(lead.Address.State), actingUserId, now);
        installations.Add(installation);

        var outcome = await unitOfWork.SaveAsync();
        if (outcome != SaveOutcome.Saved)
            return Failure(outcome, id);
        await transaction.CommitAsync();
        return Result.Success(LeadResponse.From(lead, customer, installation.Id));
    }

    public async Task<Result<LeadResponse>> UpdateAsync(int id, UpdateLeadRequest request)
    {
        var lead = await leads.GetByIdAsync(id);
        if (lead is null)
            return Result.Failure<LeadResponse>(LeadErrors.NotFound(id));
        if (!lead.IsOpen)
            return Result.Failure<LeadResponse>(LeadErrors.Closed(lead.Status));

        // BR-L1, checked in the service like the quote form's (the naive version; the fix is a unique index).
        if (await leads.FindOpenIdByEmailAsync(request.Email, exceptId: id) is not null)
            return Result.Failure<LeadResponse>(LeadErrors.EmailTaken);

        lead.FirstName = request.FirstName.Trim();
        lead.LastName = request.LastName.Trim();
        lead.Email = request.Email.Trim();
        lead.PhoneNumber = request.Phone.Trim();
        return await SaveAsync(lead);
    }

    private async Task<Result<LeadResponse>> SaveAsync(Lead lead)
    {
        var outcome = await unitOfWork.SaveAsync();
        return outcome == SaveOutcome.Saved
            ? Result.Success(await DetailsAsync(lead))
            : Failure(outcome, lead.Id);
    }

    // Leads are never deleted by the API and nothing a move writes is unique, so a save fails only if the row is gone.
    private static Result<LeadResponse> Failure(SaveOutcome outcome, int id) => outcome switch
    {
        SaveOutcome.Conflict => Result.Failure<LeadResponse>(LeadErrors.NotFound(id)),
        _ => throw new UnreachableException($"Changing a lead can't end as {outcome}.")
    };

    // The customer a lead names: the one it was converted into or, while it is open, the one a conversion would reuse.
    private async Task<LeadResponse> DetailsAsync(Lead lead)
    {
        var customer = lead.Customer ?? (lead.IsOpen ? await customers.GetByEmailAsync(lead.Email) : null);
        var installationId = lead.Status == LeadStatus.Converted ? await installations.GetIdForLeadAsync(lead.Id) : null;
        return LeadResponse.From(lead, customer, installationId);
    }

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
