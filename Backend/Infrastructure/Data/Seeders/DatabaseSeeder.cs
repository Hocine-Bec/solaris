using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the database with sample data for testing
/// Run this once after initial migration
/// </summary>
public class DatabaseSeeder(AppDbContext context)
{
    public async Task SeedAsync()
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Users.AnyAsync())
        {
            await SeedUsers();
            await SeedAddresses();
            await SeedCustomers();
            await SeedLeads();
            await SeedInstallations();
            await SeedInstallationStatusHistory();
            await SeedInstallationTechnicians();
            await SeedEquipment();
            await SeedWeatherData();
            await SeedEnergyProduction();
            await SeedSupportTickets();
            await SeedDocuments();
        }
    }

    private async Task SeedUsers()
    {
        var users = new List<User>
        {
            new User
            {
                Email = "admin@solarcompany.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
            },
            new User
            {
                Email = "tech.john@solarcompany.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                FirstName = "John",
                LastName = "Smith",
                Role = UserRole.Technician,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                LastLoginAt = DateTime.UtcNow.AddDays(-3),
                Specialization = "Solar Panel Installation",
                LicenseNumber = "TECH-001"
            },
            new User
            {
                Email = "tech.maria@solarcompany.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                FirstName = "Maria",
                LastName = "Garcia",
                Role = UserRole.Technician,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                LastLoginAt = DateTime.UtcNow.AddDays(-5),
                Specialization = "Electrical Systems",
                LicenseNumber = "TECH-002"
            },
            new User
            {
                Email = "tech.david@solarcompany.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                FirstName = "David",
                LastName = "Chen",
                Role = UserRole.Technician,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                LastLoginAt = DateTime.UtcNow.AddDays(-2),
                Specialization = "Battery Systems",
                LicenseNumber = "TECH-003"
            },
            new User
            {
                Email = "customer.service@solarcompany.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Support123!"),
                FirstName = "Sarah",
                LastName = "Johnson",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-10),
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
    private async Task SeedAddresses()
    {
        var addresses = new List<Address>
        {
            new Address
            {
                Street = "123 Main Street",
                City = "San Diego",
                State = "CA",
                ZipCode = "92101",
                Country = "USA",
                Latitude = 32.7157m,
                Longitude = -117.1611m,
                UniqueAddressHash = Address.BuildHash("123 Main Street", "San Diego", "CA", "92101", "USA")
            },
            new Address
            {
                Street = "456 Oak Avenue",
                City = "Los Angeles",
                State = "CA",
                ZipCode = "90001",
                Country = "USA",
                Latitude = 34.0522m,
                Longitude = -118.2437m,
                UniqueAddressHash = Address.BuildHash("456 Oak Avenue", "Los Angeles", "CA", "90001", "USA")
            },
            new Address
            {
                Street = "789 Pine Road",
                City = "San Francisco",
                State = "CA",
                ZipCode = "94102",
                Country = "USA",
                Latitude = 37.7749m,
                Longitude = -122.4194m,
                UniqueAddressHash = Address.BuildHash("789 Pine Road", "San Francisco", "CA", "94102", "USA")
            },
            new Address
            {
                Street = "321 Elm Street",
                City = "San Diego",
                State = "CA",
                ZipCode = "92103",
                Country = "USA",
                Latitude = 32.7490m,
                Longitude = -117.1670m,
                UniqueAddressHash = Address.BuildHash("321 Elm Street", "San Diego", "CA", "92103", "USA")
            },
            new Address
            {
                Street = "654 Maple Drive",
                City = "San Jose",
                State = "CA",
                ZipCode = "95123",
                Country = "USA",
                Latitude = 37.3382m,
                Longitude = -121.8863m,
                UniqueAddressHash = Address.BuildHash("654 Maple Drive", "San Jose", "CA", "95123", "USA")
            }
        };

        await context.Addresses.AddRangeAsync(addresses);
        await context.SaveChangesAsync();
    }
    private async Task SeedCustomers()
    {
        var customers = new List<Customer>
        {
            new Customer
            {
                FirstName = "Robert",
                LastName = "Wilson",
                Email = "robert.wilson@email.com",
                PhoneNumber = "(619) 555-0101",
                Status = CustomerStatus.Active,
                RegistrationDate = DateTime.UtcNow.AddMonths(-6),
                LastActivityDate = DateTime.UtcNow.AddDays(-10),
                ContactAddressId = 1
            },
            new Customer
            {
                FirstName = "Jennifer",
                LastName = "Martinez",
                Email = "jennifer.martinez@email.com",
                PhoneNumber = "(213) 555-0102",
                Status = CustomerStatus.Active,
                RegistrationDate = DateTime.UtcNow.AddMonths(-4),
                LastActivityDate = DateTime.UtcNow.AddDays(-5),
                ContactAddressId = 2
            },
            new Customer
            {
                FirstName = "Michael",
                LastName = "Brown",
                Email = "michael.brown@email.com",
                PhoneNumber = "(415) 555-0103",
                Status = CustomerStatus.Active,
                RegistrationDate = DateTime.UtcNow.AddMonths(-2),
                LastActivityDate = DateTime.UtcNow.AddDays(-2),
                ContactAddressId = 3
            },
            new Customer
            {
                FirstName = "Lisa",
                LastName = "Taylor",
                Email = "lisa.taylor@email.com",
                PhoneNumber = "(619) 555-0104",
                Status = CustomerStatus.Prospect,
                RegistrationDate = DateTime.UtcNow.AddMonths(-1),
                LastActivityDate = DateTime.UtcNow.AddDays(-15),
                ContactAddressId = 4
            }
        };

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();
    }
    private async Task SeedLeads()
    {
        var leads = new List<Lead>
        {
            new Lead
            {
                FirstName = "Lisa",
                LastName = "Taylor",
                Email = "lisa.taylor@email.com",
                PhoneNumber = "(619) 555-0104",
                AddressId = 4,
                PropertyType = PropertyType.House,
                IsPropertyOwner = true,
                Status = LeadStatus.Converted,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                ContactedAt = DateTime.UtcNow.AddMonths(-2).AddDays(1),
                ConvertedAt = DateTime.UtcNow.AddMonths(-1),
                MonthlyBillRange = "$200-$300",
                BestTimeToContact = "Evening",
                Notes = "Very interested in battery backup system",
                CustomerId = 4
            },
            new Lead
            {
                FirstName = "James",
                LastName = "Anderson",
                Email = "james.anderson@email.com",
                PhoneNumber = "(408) 555-0105",
                AddressId = 5,
                PropertyType = PropertyType.House,
                IsPropertyOwner = true,
                Status = LeadStatus.Contacted,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ContactedAt = DateTime.UtcNow.AddDays(-8),
                MonthlyBillRange = "$150-$250",
                BestTimeToContact = "Morning",
                Notes = "Considering solar for new construction"
            },
            new Lead
            {
                FirstName = "Amanda",
                LastName = "White",
                Email = "amanda.white@email.com",
                PhoneNumber = "(619) 555-0106",
                AddressId = 1,
                PropertyType = PropertyType.Apartment,
                IsPropertyOwner = false,
                Status = LeadStatus.Disqualified,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                ContactedAt = DateTime.UtcNow.AddDays(-18),
                MonthlyBillRange = "$100-$150",
                BestTimeToContact = "Afternoon",
                Notes = "Renter - not eligible for installation"
            },
            new Lead
            {
                FirstName = "Kevin",
                LastName = "Lee",
                Email = "kevin.lee@email.com",
                PhoneNumber = "(213) 555-0107",
                AddressId = 2,
                PropertyType = PropertyType.Commercial,
                IsPropertyOwner = true,
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                MonthlyBillRange = "$500-$700",
                BestTimeToContact = "Business Hours",
                Notes = "Commercial property owner interested in large system"
            }
        };

        await context.Leads.AddRangeAsync(leads);
        await context.SaveChangesAsync();
    }
    private async Task SeedInstallations()
    {
        var installations = new List<Installation>
        {
            new Installation
            {
                ProjectName = "Wilson Residence Solar Project",
                Status = InstallationStatus.Active,
                StartDate = DateTime.UtcNow.AddMonths(-5),
                CompletionDate = DateTime.UtcNow.AddMonths(-1),
                SystemSizeKw = 8.5m,
                PanelCount = 22,
                InverterType = "String Inverter",
                Notes = "South-facing roof, excellent sun exposure",
                CustomerId = 1,
                InstallationAddressId = 1,
                Customer = null!,
                InstallationAddress = null!
            },
            new Installation
            {
                ProjectName = "Martinez Home Solar System",
                Status = InstallationStatus.Inspection,
                StartDate = DateTime.UtcNow.AddMonths(-3),
                CompletionDate = null,
                SystemSizeKw = 12.2m,
                PanelCount = 32,
                InverterType = "Microinverters",
                Notes = "Includes battery backup system",
                CustomerId = 2,
                InstallationAddressId = 2,
                Customer = null!,
                InstallationAddress = null!
            },
            new Installation
            {
                ProjectName = "Brown Family Solar Installation",
                Status = InstallationStatus.Installation,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                CompletionDate = null,
                SystemSizeKw = 6.8m,
                PanelCount = 18,
                InverterType = "String Inverter",
                Notes = "East-west roof configuration",
                CustomerId = 3,
                InstallationAddressId = 3,
                Customer = null!,
                InstallationAddress = null!
            },
            new Installation
            {
                ProjectName = "Taylor Residence Solar Project",
                Status = InstallationStatus.Design,
                StartDate = DateTime.UtcNow.AddDays(-10),
                CompletionDate = null,
                SystemSizeKw = 10.0m,
                PanelCount = 26,
                InverterType = "Microinverters",
                Notes = "Planning phase - awaiting customer approval",
                CustomerId = 4,
                InstallationAddressId = 4,
                Customer = null!,
                InstallationAddress = null!
            }
        };

        await context.Installations.AddRangeAsync(installations);
        await context.SaveChangesAsync();
    }
    private async Task SeedInstallationStatusHistory()
    {
        var adminUser = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var history = new List<InstallationStatusHistory>();

        // Installation 1 - Complete workflow
        var installation1History = new[]
        {
            new { From = InstallationStatus.Survey, To = InstallationStatus.Design, DaysAgo = 150 },
            new { From = InstallationStatus.Design, To = InstallationStatus.Permits, DaysAgo = 140 },
            new { From = InstallationStatus.Permits, To = InstallationStatus.Installation, DaysAgo = 120 },
            new { From = InstallationStatus.Installation, To = InstallationStatus.Inspection, DaysAgo = 60 },
            new { From = InstallationStatus.Inspection, To = InstallationStatus.Active, DaysAgo = 30 }
        };

        foreach (var change in installation1History)
        {
            history.Add(new InstallationStatusHistory
            {
                FromStatus = change.From,
                ToStatus = change.To,
                ChangedAt = DateTime.UtcNow.AddDays(-change.DaysAgo),
                Notes = $"Status changed from {change.From} to {change.To}",
                InstallationId = 1,
                ChangedByUserId = adminUser.Id,
                Installation = null!,
                ChangedBy = null!
            });
        }

        // Installation 2 - In progress
        var installation2History = new[]
        {
            new { From = InstallationStatus.Survey, To = InstallationStatus.Design, DaysAgo = 90 },
            new { From = InstallationStatus.Design, To = InstallationStatus.Permits, DaysAgo = 75 },
            new { From = InstallationStatus.Permits, To = InstallationStatus.Installation, DaysAgo = 45 },
            new { From = InstallationStatus.Installation, To = InstallationStatus.Inspection, DaysAgo = 10 }
        };

        foreach (var change in installation2History)
        {
            history.Add(new InstallationStatusHistory
            {
                FromStatus = change.From,
                ToStatus = change.To,
                ChangedAt = DateTime.UtcNow.AddDays(-change.DaysAgo),
                Notes = $"Status changed from {change.From} to {change.To}",
                InstallationId = 2,
                ChangedByUserId = adminUser.Id,
                Installation = null!,
                ChangedBy = null!
            });
        }

        await context.InstallationStatusHistories.AddRangeAsync(history);
        await context.SaveChangesAsync();
    }
    private async Task SeedInstallationTechnicians()
    {
        var technicians = await context.Users.Where(u => u.Role == UserRole.Technician).ToListAsync();
        var assignments = new List<InstallationTechnician>();

        // Installation 1 - Completed project
        assignments.Add(new InstallationTechnician
        {
            AssignedDate = DateTime.UtcNow.AddMonths(-5),
            CompletedDate = DateTime.UtcNow.AddMonths(-1),
            Role = "Lead Installer",
            Notes = "Completed installation ahead of schedule",
            InstallationId = 1,
            TechnicianId = technicians[0].Id,
            Installation = null!,
            Technician = null!
        });

        // Installation 2 - Current project with multiple technicians
        assignments.Add(new InstallationTechnician
        {
            AssignedDate = DateTime.UtcNow.AddMonths(-3),
            CompletedDate = null,
            Role = "Lead Installer",
            Notes = "Managing battery system installation",
            InstallationId = 2,
            TechnicianId = technicians[1].Id,
            Installation = null!,
            Technician = null!
        });

        assignments.Add(new InstallationTechnician
        {
            AssignedDate = DateTime.UtcNow.AddMonths(-2),
            CompletedDate = null,
            Role = "Electrical Specialist",
            Notes = "Handling electrical connections",
            InstallationId = 2,
            TechnicianId = technicians[2].Id,
            Installation = null!,
            Technician = null!
        });

        // Installation 3 - New project
        assignments.Add(new InstallationTechnician
        {
            AssignedDate = DateTime.UtcNow.AddMonths(-1),
            CompletedDate = null,
            Role = "Installer",
            Notes = "Standard panel installation",
            InstallationId = 3,
            TechnicianId = technicians[0].Id,
            Installation = null!,
            Technician = null!
        });

        await context.InstallationTechnicians.AddRangeAsync(assignments);
        await context.SaveChangesAsync();
    }
    private async Task SeedEquipment()
    {
        var equipment = new List<Equipment>
        {
            // Solar Panels
            new Equipment
            {
                Type = EquipmentType.SolarPanel,
                Model = "SunPower X22-370",
                SerialNumber = "SPX22-370-001",
                Manufacturer = "SunPower",
                Status = EquipmentStatus.Installed,
                PurchaseDate = DateTime.UtcNow.AddMonths(-6),
                WarrantyExpiryDate = DateTime.UtcNow.AddYears(24).AddMonths(6),
                Cost = 285.00m,
                Specifications = "{\"wattage\": 370, \"efficiency\": 22.1, \"cellType\": \"Monocrystalline\"}",
                InstallationId = 1
            },
            new Equipment
            {
                Type = EquipmentType.SolarPanel,
                Model = "LG NeON R",
                SerialNumber = "LGN360-002",
                Manufacturer = "LG",
                Status = EquipmentStatus.Installed,
                PurchaseDate = DateTime.UtcNow.AddMonths(-4),
                WarrantyExpiryDate = DateTime.UtcNow.AddYears(24).AddMonths(8),
                Cost = 265.00m,
                Specifications = "{\"wattage\": 360, \"efficiency\": 21.7, \"cellType\": \"N-type\"}",
                InstallationId = 2
            },

            // Inverters
            new Equipment
            {
                Type = EquipmentType.Inverter,
                Model = "SE7600H",
                SerialNumber = "SE7600-001",
                Manufacturer = "SolarEdge",
                Status = EquipmentStatus.Installed,
                PurchaseDate = DateTime.UtcNow.AddMonths(-6),
                WarrantyExpiryDate = DateTime.UtcNow.AddYears(11).AddMonths(6),
                Cost = 1200.00m,
                Specifications = "{\"power\": 7600, \"efficiency\": 99.0, \"type\": \"String\"}",
                InstallationId = 1
            },
            new Equipment
            {
                Type = EquipmentType.Inverter,
                Model = "IQ8+",
                SerialNumber = "ENPH-IQ8-001",
                Manufacturer = "Enphase",
                Status = EquipmentStatus.Assigned,
                PurchaseDate = DateTime.UtcNow.AddMonths(-3),
                WarrantyExpiryDate = DateTime.UtcNow.AddYears(24).AddMonths(9),
                Cost = 185.00m,
                Specifications = "{\"power\": 300, \"efficiency\": 97.5, \"type\": \"Microinverter\"}",
                InstallationId = 2
            },

            // Batteries
            new Equipment
            {
                Type = EquipmentType.Battery,
                Model = "Powerwall 2",
                SerialNumber = "PW2-001",
                Manufacturer = "Tesla",
                Status = EquipmentStatus.Assigned,
                PurchaseDate = DateTime.UtcNow.AddMonths(-3),
                WarrantyExpiryDate = DateTime.UtcNow.AddYears(9).AddMonths(9),
                Cost = 7500.00m,
                Specifications = "{\"capacity\": 13.5, \"power\": 7, \"chemistry\": \"Lithium-ion\"}",
                InstallationId = 2
            },

            // In-stock equipment
            new Equipment
            {
                Type = EquipmentType.SolarPanel,
                Model = "Q.PEAK DUO BLK-G10",
                SerialNumber = "QCELLS-001",
                Manufacturer = "Qcells",
                Status = EquipmentStatus.InStock,
                PurchaseDate = DateTime.UtcNow.AddMonths(-1),
                WarrantyExpiryDate = DateTime.UtcNow.AddYears(24).AddMonths(11),
                Cost = 240.00m,
                Specifications = "{\"wattage\": 355, \"efficiency\": 20.6, \"cellType\": \"PERC\"}",
                InstallationId = null
            }
        };

        await context.Equipment.AddRangeAsync(equipment);
        await context.SaveChangesAsync();
    }
    private async Task SeedWeatherData()
    {
        var weatherData = new List<WeatherData>
        {
            new WeatherData
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                Location = "San Diego, CA",
                Condition = "Sunny",
                TemperatureCelsius = 24.5m,
                CloudCoverPercentage = 10
            },
            new WeatherData
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)),
                Location = "San Diego, CA",
                Condition = "Partly Cloudy",
                TemperatureCelsius = 22.8m,
                CloudCoverPercentage = 40
            },
            new WeatherData
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
                Location = "San Diego, CA",
                Condition = "Cloudy",
                TemperatureCelsius = 20.1m,
                CloudCoverPercentage = 75
            },
            new WeatherData
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                Location = "San Diego, CA",
                Condition = "Sunny",
                TemperatureCelsius = 25.2m,
                CloudCoverPercentage = 5
            },
            new WeatherData
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                Location = "San Diego, CA",
                Condition = "Rainy",
                TemperatureCelsius = 18.3m,
                CloudCoverPercentage = 90
            }
        };

        await context.WeatherData.AddRangeAsync(weatherData);
        await context.SaveChangesAsync();
    }
    private async Task SeedEnergyProduction()
    {
        var production = new List<EnergyProduction>();
        var installation1 = await context.Installations.FindAsync(1);
        var weatherData = await context.WeatherData.ToListAsync();

        // Generate 30 days of production data for installation 1
        for (int i = 0; i < 30; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-29 + i));
            var weather = weatherData.FirstOrDefault(w => w.Date == date);
            
            // Simulate seasonal variation + weather impact
            var baseProduction = installation1!.SystemSizeKw * 4.2m; // 4.2 sun hours average
            var weatherMultiplier = weather?.Condition switch
            {
                "Sunny" => 1.1m,
                "Partly Cloudy" => 0.9m,
                "Cloudy" => 0.6m,
                "Rainy" => 0.3m,
                _ => 0.8m
            };
            
            var randomVariation = (decimal)(Random.Shared.NextDouble() * 0.4 + 0.8); // 0.8-1.2 variation
            var actualProduction = baseProduction * weatherMultiplier * randomVariation;

            production.Add(new EnergyProduction
            {
                ProductionDate = date,
                ActualProductionKwh = Math.Round(actualProduction,
                    2),
                ExpectedProductionKwh = Math.Round(baseProduction,
                    2),
                HealthStatus = actualProduction >= baseProduction * 0.9m
                    ? SystemHealthStatus.Excellent
                    : actualProduction >= baseProduction * 0.7m
                        ? SystemHealthStatus.Good
                        : SystemHealthStatus.Warning,
                Notes = weather?.Condition ?? "Normal operation",
                InstallationId = 1,
                WeatherDataId = weather?.Id,
                Installation = null!
            });
        }

        await context.EnergyProductions.AddRangeAsync(production);
        await context.SaveChangesAsync();
    }
    private async Task SeedSupportTickets()
    {
        var supportUser = await context.Users.FirstAsync(u => u.Email == "customer.service@solarcompany.com");
        var tickets = new List<SupportTicket>
        {
            new SupportTicket
            {
                Title = "System performance lower than expected",
                Description =
                    "Noticed that energy production seems about 15% lower than what was projected during the design phase.",
                Status = TicketStatus.Resolved,
                Priority = TicketPriority.Medium,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                AssignedAt = DateTime.UtcNow.AddDays(-19),
                ResolvedAt = DateTime.UtcNow.AddDays(-15),
                ResolutionNotes = "Adjusted panel tilt and cleaned debris. Performance returned to expected levels.",
                CustomerId = 1,
                AssignedToUserId = supportUser.Id,
                InstallationId = 1,
                Customer = null!
            },
            new SupportTicket
            {
                Title = "Monitoring app not showing real-time data",
                Description = "The solar monitoring application shows data from 2 days ago but not current production.",
                Status = TicketStatus.InProgress,
                Priority = TicketPriority.High,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                AssignedAt = DateTime.UtcNow.AddDays(-4),
                ResolutionNotes = null,
                CustomerId = 2,
                AssignedToUserId = supportUser.Id,
                InstallationId = 2,
                Customer = null!
            },
            new SupportTicket
            {
                Title = "Inverter error code 023",
                Description = "Inverter display shows error code 023 and system has stopped producing power.",
                Status = TicketStatus.Open,
                Priority = TicketPriority.Critical,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                AssignedAt = null,
                ResolutionNotes = null,
                CustomerId = 3,
                AssignedToUserId = null,
                InstallationId = 3,
                Customer = null!
            },
            new SupportTicket
            {
                Title = "General inquiry about expansion",
                Description = "Interested in adding more panels to existing system. Need information about compatibility and costs.",
                Status = TicketStatus.Open,
                Priority = TicketPriority.Low,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                AssignedAt = null,
                ResolutionNotes = null,
                CustomerId = 1,
                AssignedToUserId = null,
                InstallationId = 1,
                Customer = null!
            }
        };

        await context.SupportTickets.AddRangeAsync(tickets);
        await context.SaveChangesAsync();
    }
    private async Task SeedDocuments()
    {
        var adminUser = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var documents = new List<Document>
        {
            // Customer documents
            new Document
            {
                FileName = "robert_wilson_id.pdf",
                FilePath = "/documents/customers/1/id_verification.pdf",
                FileType = "application/pdf",
                FileSize = 2048576,
                Type = DocumentType.IdVerification,
                UploadedAt = DateTime.UtcNow.AddMonths(-6),
                Description = "Driver's License - Robert Wilson",
                CustomerId = 1,
                UploadedByUserId = adminUser.Id,
                UploadedBy = null!
            },
            new Document
            {
                FileName = "wilson_utility_bill.pdf",
                FilePath = "/documents/customers/1/utility_bill.pdf",
                FileType = "application/pdf",
                FileSize = 1548576,
                Type = DocumentType.UtilityBill,
                UploadedAt = DateTime.UtcNow.AddMonths(-6),
                Description = "SDG&E Utility Bill",
                CustomerId = 1,
                UploadedByUserId = adminUser.Id,
                UploadedBy = null!
            },

            // Installation documents
            new Document
            {
                FileName = "wilson_building_permit.pdf",
                FilePath = "/documents/installations/1/building_permit.pdf",
                FileType = "application/pdf",
                FileSize = 3048576,
                Type = DocumentType.BuildingPermit,
                UploadedAt = DateTime.UtcNow.AddMonths(-5),
                Description = "City of San Diego Building Permit",
                InstallationId = 1,
                UploadedByUserId = adminUser.Id,
                UploadedBy = null!
            },
            new Document
            {
                FileName = "wilson_installation_photo1.jpg",
                FilePath = "/documents/installations/1/installation_photo1.jpg",
                FileType = "image/jpeg",
                FileSize = 4458576,
                Type = DocumentType.InstallationPhoto,
                UploadedAt = DateTime.UtcNow.AddMonths(-2),
                Description = "Roof installation completed",
                InstallationId = 1,
                UploadedByUserId = adminUser.Id,
                UploadedBy = null!
            },
            new Document
            {
                FileName = "wilson_warranty.pdf",
                FilePath = "/documents/installations/1/warranty.pdf",
                FileType = "application/pdf",
                FileSize = 1048576,
                Type = DocumentType.WarrantyDocument,
                UploadedAt = DateTime.UtcNow.AddMonths(-1),
                Description = "25-Year System Warranty",
                InstallationId = 1,
                UploadedByUserId = adminUser.Id,
                UploadedBy = null!
            },

            // Ticket documents
            new Document
            {
                FileName = "performance_report.pdf",
                FilePath = "/documents/tickets/1/performance_report.pdf",
                FileType = "application/pdf",
                FileSize = 1848576,
                Type = DocumentType.DiagnosticReport,
                UploadedAt = DateTime.UtcNow.AddDays(-18),
                Description = "System Performance Analysis",
                TicketId = 1,
                UploadedByUserId = adminUser.Id,
                UploadedBy = null!
            }
        };

        await context.Documents.AddRangeAsync(documents);
        await context.SaveChangesAsync();
    }
}