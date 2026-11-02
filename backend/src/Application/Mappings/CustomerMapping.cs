using Domain.Entities;
using Domain.Enums;
using Mapster;
using Application.DTOs.Customer;

namespace Application.Mappings;

public class CustomerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to CustomerResponse
        config.NewConfig<Customer, CustomerResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ContactAddress, src => src.ContactAddress != null
                ? $"{src.ContactAddress.Street}, {src.ContactAddress.City}, {src.ContactAddress.State} {src.ContactAddress.ZipCode}"
                : null)
            .Map(dest => dest.InstallationCount, src => src.Installations.Count);

        // Entity to CustomerListResponse
        config.NewConfig<Customer, CustomerListResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ActiveInstallations, src => src.Installations.Count(i => i.Status == InstallationStatus.Active));

        // Entity to CustomerDetailResponse
        config.NewConfig<Customer, CustomerDetailResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ContactAddress, src => src.ContactAddress != null
                ? $"{src.ContactAddress.Street}, {src.ContactAddress.City}, {src.ContactAddress.State}"
                : null)
            .Map(dest => dest.Installations, src => src.Installations)
            .Map(dest => dest.Documents, src => src.Documents)
            .Map(dest => dest.RecentTickets, src => src.SupportTickets.OrderByDescending(t => t.CreatedAt).Take(5));

        // CreateCustomerRequest to Entity
        config.NewConfig<CreateCustomerRequest, Customer>()
            .Map(dest => dest.RegistrationDate, src => DateTime.UtcNow)
            .Map(dest => dest.Status, src => CustomerStatus.Prospect)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.LastActivityDate)
            .Ignore(dest => dest.Installations)
            .Ignore(dest => dest.Documents)
            .Ignore(dest => dest.SupportTickets)
            .Ignore(dest => dest.ContactAddress);

        // UpdateCustomerRequest to Entity
        config.NewConfig<UpdateCustomerRequest, Customer>()
            .IgnoreNullValues(true)
            .Map(dest => dest.Status, src => src.Status != null ? Enum.Parse<CustomerStatus>(src.Status) : CustomerStatus.Prospect)
            .Map(dest => dest.LastActivityDate, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.RegistrationDate)
            .Ignore(dest => dest.Installations)
            .Ignore(dest => dest.Documents)
            .Ignore(dest => dest.SupportTickets)
            .Ignore(dest => dest.ContactAddress);
    }
}