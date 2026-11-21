using Application.Interfaces.Repositories;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CustomerRepo(AppDbContext context) : GenericRepo<Customer>(context), ICustomerRepo
{
    // As addresses: the unique index on the normalized email decides between two conversions at the same moment.
    public async Task<Customer> GetOrAddAsync(Customer customer)
    {
        await Context.Database.ExecuteSqlAsync($"""
            INSERT INTO customers (first_name, last_name, email, normalized_email, phone_number, status,
                                   registration_date, contact_address_id)
            VALUES ({customer.FirstName}, {customer.LastName}, {customer.Email}, {customer.NormalizedEmail},
                    {customer.PhoneNumber}, {customer.Status.ToString()}, {customer.RegistrationDate},
                    {customer.ContactAddressId})
            ON CONFLICT (normalized_email) DO NOTHING
            """);
        return await Context.Customers.SingleAsync(c => c.NormalizedEmail == customer.NormalizedEmail);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        var normalized = Emails.Normalize(email);
        return await Context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.NormalizedEmail == normalized);
    }

    public async Task<(IReadOnlyList<(Customer Customer, int InstallationCount)> Items, int TotalCount)> GetPageAsync(
        string? search, int skip, int take)
    {
        var query = Context.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // A search for "50%" or "a_b" finds those characters, not any text: LIKE's wildcards are escaped.
            var pattern = $"%{search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            query = query.Where(c => EF.Functions.ILike(c.FirstName + " " + c.LastName, pattern, @"\")
                                     || EF.Functions.ILike(c.Email, pattern, @"\"));
        }

        var totalCount = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .Select(c => new { Customer = c, InstallationCount = c.Installations.Count })
            .ToListAsync();
        return (rows.Select(r => (r.Customer, r.InstallationCount)).ToList(), totalCount);
    }

    public async Task<Customer?> GetWithInstallationsAsync(int id)
    {
        return await Context.Customers
            .AsNoTracking()
            .Include(c => c.ContactAddress)
            .Include(c => c.Installations.OrderByDescending(i => i.Id)).ThenInclude(i => i.Address)
            .Include(c => c.Installations.OrderByDescending(i => i.Id)).ThenInclude(i => i.Lead)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
