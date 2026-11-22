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
// A lead read by ID is tracked, so saving writes only the columns a move or a correction changed, and only if nobody
// saved the lead since it was read (its version, BR-L3): otherwise the change is refused as a concurrent one.
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
        var lead = request.Adapt<Lead>();
        lead.NormalizedEmail = Emails.Normalize(lead.Email);
        lead.Status = LeadStatus.New;
        lead.CreatedAt = Now();
        var address = lead.Address;
        address.UniqueAddressHash = Address.BuildHash(address.Street, address.City, address.State, address.ZipCode,
            address.Country);

        // The property (stored once, by its key), the lead and its two notifications commit together, or not at all
        // (BR-N1). BR-L1 is the database's: a second open lead for this email breaks a partial unique index, even when
        // the same form is sent many times at once; then everything rolls back, so no duplicate email exists either.
        SaveOutcome outcome;
        await using (var transaction = await unitOfWork.BeginTransactionAsync())
        {
            lead.Address = await addresses.GetOrAddAsync(address);
            leads.Add(lead);
            notifier.OnSaving(lead);
            outcome = await unitOfWork.SaveAsync();
            if (outcome == SaveOutcome.Saved)
                await transaction.CommitAsync();
        }

        if (outcome == SaveOutcome.Duplicate)
            return new LeadCreated(await leads.GetIdOfExistingAsync(lead.NormalizedEmail), IsNew: false);
        if (outcome != SaveOutcome.Saved)
            throw new UnreachableException($"Adding a lead can't end as {outcome}.");

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
            return Failure(outcome);
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

        lead.FirstName = request.FirstName.Trim();
        lead.LastName = request.LastName.Trim();
        lead.Email = request.Email.Trim();
        lead.NormalizedEmail = Emails.Normalize(request.Email);
        lead.PhoneNumber = request.Phone.Trim();
        // BR-L1: another open lead with this email breaks the partial unique index
        return await SaveAsync(lead);
    }

    private async Task<Result<LeadResponse>> SaveAsync(Lead lead)
    {
        var outcome = await unitOfWork.SaveAsync();
        return outcome == SaveOutcome.Saved
            ? Result.Success(await DetailsAsync(lead))
            : Failure(outcome);
    }

    // A version that moved: someone saved the lead since it was read. A unique index: only the open-email one (BR-L1).
    private static Result<LeadResponse> Failure(SaveOutcome outcome) => outcome switch
    {
        SaveOutcome.Conflict => Result.Failure<LeadResponse>(LeadErrors.ConcurrentChange),
        SaveOutcome.Duplicate => Result.Failure<LeadResponse>(LeadErrors.EmailTaken),
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
