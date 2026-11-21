using Application.DTOs.Common;
using Domain.Entities;

namespace Application.DTOs.Customers;

/// <summary>A customer in the list (FR-C1): who, their status, how many installations, since when.</summary>
public sealed record CustomerSummaryResponse(
    int Id,
    string Name,
    string Email,
    string Phone,
    string Status,
    int InstallationCount,
    DateTime CreatedAt)
{
    public static CustomerSummaryResponse From(Customer customer, int installationCount) =>
        new(customer.Id, customer.FullName, customer.Email, customer.PhoneNumber, customer.Status.ToString(),
            installationCount, customer.RegistrationDate);
}

/// <summary>One customer, as their page shows them (FR-C1): contact, status (BR-C1), and their installations.</summary>
public sealed record CustomerResponse(
    int Id,
    string FirstName,
    string LastName,
    string Name,
    string Email,
    string Phone,
    AddressResponse? Address,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<CustomerInstallationResponse> Installations)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id, customer.FirstName, customer.LastName, customer.FullName, customer.Email, customer.PhoneNumber,
            customer.ContactAddress is null ? null : AddressResponse.From(customer.ContactAddress),
            customer.Status.ToString(), customer.RegistrationDate,
            customer.Installations.Select(CustomerInstallationResponse.From).ToList());
}

/// <summary>
/// One of a customer's installations: where, its stage, when it started, and the lead it came from, with when that
/// request was received.
/// </summary>
public sealed record CustomerInstallationResponse(
    int Id,
    AddressResponse Address,
    string Status,
    DateTime CreatedAt,
    int LeadId,
    DateTime LeadReceivedAt)
{
    public static CustomerInstallationResponse From(Installation installation) =>
        new(installation.Id, AddressResponse.From(installation.Address), installation.Status.ToString(),
            installation.CreatedAt, installation.LeadId, installation.Lead.CreatedAt);
}
