using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICustomerRepo : IGenericRepo<Customer>
{
    /// <summary>
    /// The customer with this customer's email (compared normalized), inserted first if there is none, and tracked.
    /// The insert can't be duplicated (<c>ON CONFLICT DO NOTHING</c>, then a read), so two conversions for one email at
    /// the same moment make one customer. It runs at once: call it inside the operation's transaction.
    /// </summary>
    Task<Customer> GetOrAddAsync(Customer customer);

    /// <summary>The customer with this email, compared as <c>Emails.Normalize</c> does; null if there is none.</summary>
    Task<Customer?> GetByEmailAsync(string email);

    /// <summary>
    /// One page of customers whose name or email contains <paramref name="search"/>, whatever its case (all of them
    /// when it is empty), newest first, each with its number of installations, and how many customers match.
    /// </summary>
    Task<(IReadOnlyList<(Customer Customer, int InstallationCount)> Items, int TotalCount)> GetPageAsync(string? search,
        int skip, int take);

    /// <summary>A customer with its address and its installations (and theirs), newest installation first.</summary>
    Task<Customer?> GetWithInstallationsAsync(int id);
}
