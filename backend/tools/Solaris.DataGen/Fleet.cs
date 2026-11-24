using System.Globalization;
using Application.Common.Security;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Solaris.DataGen;

/// <summary>One generated installation, with what the readings need of it.</summary>
public sealed record GeneratedInstallation(
    int Id,
    string State,
    string TimeZone,
    decimal SystemSizeKw,
    SystemCondition Condition,
    string DeviceKey);

/// <summary>
/// The Active installations a measurement run needs, spread across the states so the fleet covers every time zone
/// (D18): each one a customer, an address, a converted lead and the installation itself, with its device key.
/// </summary>
public static class Fleet
{
    /// <summary>Every state, in a fixed order, so a seed always lands the same installations in the same places.</summary>
    private static readonly string[] States = UsStates.Codes.Order(StringComparer.Ordinal).ToArray();

    public static async Task<IReadOnlyList<GeneratedInstallation>> CreateAsync(AppDbContext context, Options options, Random random, DateTime activatedAt)
    {
        var made = new List<GeneratedInstallation>(options.Installations);
        var addresses = new List<Address>(options.Installations);
        var customers = new List<Customer>(options.Installations);

        for (var n = 0; n < options.Installations; n++)
        {
            // Round the states, so every zone is covered whatever the fleet's size
            var state = States[n % States.Length];
            var address = new Address
            {
                Street = $"{100 + n} Measurement Way",
                City = "Testville",
                State = state,
                ZipCode = (10000 + n % 89999).ToString(CultureInfo.InvariantCulture),
                Country = "US"
            };
            address.UniqueAddressHash = Address.BuildHash(address.Street, address.City, address.State, address.ZipCode, address.Country);
            addresses.Add(address);

            var email = $"measurement{n}@example.test";
            customers.Add(new Customer
            {
                FirstName = "Measurement",
                LastName = $"Customer {n}",
                Email = email,
                NormalizedEmail = Emails.Normalize(email),
                PhoneNumber = "+1 512 555 0100",
                Status = CustomerStatus.Active,
                RegistrationDate = activatedAt
            });
        }

        context.Addresses.AddRange(addresses);
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        var leads = new List<Lead>(options.Installations);
        for (var n = 0; n < options.Installations; n++)
        {
            var email = customers[n].Email;
            leads.Add(new Lead
            {
                FirstName = "Measurement",
                LastName = $"Customer {n}",
                Email = email,
                NormalizedEmail = Emails.Normalize(email),
                PhoneNumber = "+1 512 555 0100",
                AddressId = addresses[n].Id,
                PropertyType = PropertyType.House,
                IsPropertyOwner = true,
                Status = LeadStatus.Converted,
                CreatedAt = activatedAt,
                ClosedAt = activatedAt,
                CustomerId = customers[n].Id
            });
        }
        context.Leads.AddRange(leads);
        await context.SaveChangesAsync();

        var installations = new List<Installation>(options.Installations);
        var keys = new List<string>(options.Installations);
        for (var n = 0; n < options.Installations; n++)
        {
            var key = DeviceKeys.Create();
            keys.Add(key.Text);
            installations.Add(new Installation
            {
                CustomerId = customers[n].Id,
                AddressId = addresses[n].Id,
                LeadId = leads[n].Id,
                Status = InstallationStatus.Active,
                TimeZone = UsTimeZones.ProposedFor(addresses[n].State),
                // 4 to 12 kW, to a tenth
                SystemSizeKw = Math.Round(4m + (decimal)random.NextDouble() * 8m, 1),
                PanelCount = 10 + random.Next(0, 30),
                InverterType = "Measurement inverter",
                CreatedAt = activatedAt,
                StatusChangedAt = activatedAt,
                ActivatedAt = activatedAt,
                DeviceKeyPrefix = key.Prefix,
                DeviceKeyHash = key.Hash,
                DeviceKeyCreatedAt = activatedAt
            });
        }
        context.Installations.AddRange(installations);
        await context.SaveChangesAsync();

        for (var n = 0; n < options.Installations; n++)
            made.Add(new GeneratedInstallation(installations[n].Id, addresses[n].State, installations[n].TimeZone,
                installations[n].SystemSizeKw!.Value, ProductionShape.ConditionOf(random.NextDouble()), keys[n]));
        return made;
    }

    /// <summary>
    /// The keys, for the ingestion run to upload with. They are the only copies: the database keeps a hash
    /// (ADR 14). The file is for a throwaway measurement database, never a real one.
    /// </summary>
    public static async Task WriteKeysAsync(IReadOnlyList<GeneratedInstallation> fleet, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var lines = new List<string> { "installation_id,device_key" };
        lines.AddRange(fleet.Select(i => $"{i.Id.ToString(CultureInfo.InvariantCulture)},{i.DeviceKey}"));
        await File.WriteAllLinesAsync(path, lines);
    }

    /// <summary>Refuses a database that already holds readings, so a dataset is never doubled by a second run.</summary>
    public static async Task<string?> WhyNotEmptyAsync(AppDbContext context)
    {
        // Both layouts live in the same table; only its columns differ
        var readings = await context.Database
            .SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM energy_readings").ToListAsync();
        if (readings[0] > 0)
            return $"energy_readings already holds {readings[0]:N0} readings: generate into an empty database.";
        return await context.Installations.AnyAsync()
            ? "This database already has installations: generate into an empty one."
            : null;
    }
}
