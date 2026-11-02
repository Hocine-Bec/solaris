using Application.DTOs.SupportTicket;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Mappings;

public class SupportTicketMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to SupportTicketResponse
        config.NewConfig<SupportTicket, SupportTicketResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Priority, src => src.Priority.ToString())
            .Map(dest => dest.CustomerName, src => $"{src.Customer.FirstName} {src.Customer.LastName}")
            .Map(dest => dest.AssignedToUserName, src => src.AssignedTo != null
                ? $"{src.AssignedTo.FirstName} {src.AssignedTo.LastName}"
                : null)
            .Map(dest => dest.InstallationName, src => src.Installation != null ? src.Installation.ProjectName : null)
            .Map(dest => dest.DaysOpen, src => src.ClosedAt.HasValue
                ? (src.ClosedAt.Value - src.CreatedAt).Days
                : (DateTime.UtcNow - src.CreatedAt).Days);

        // Entity to SupportTicketListResponse
        config.NewConfig<SupportTicket, SupportTicketListResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Priority, src => src.Priority.ToString())
            .Map(dest => dest.CustomerName, src => $"{src.Customer.FirstName} {src.Customer.LastName}")
            .Map(dest => dest.DaysOpen, src => src.ClosedAt.HasValue
                ? (src.ClosedAt.Value - src.CreatedAt).Days
                : (DateTime.UtcNow - src.CreatedAt).Days);

        // Entity to SupportTicketSummaryResponse
        config.NewConfig<SupportTicket, SupportTicketSummaryResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Priority, src => src.Priority.ToString());

        // CreateSupportTicketRequest to Entity
        config.NewConfig<CreateSupportTicketRequest, SupportTicket>()
            .Map(dest => dest.Priority, src => Enum.Parse<TicketPriority>(src.Priority))
            .Map(dest => dest.Status, src => TicketStatus.Open)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AssignedAt)
            .Ignore(dest => dest.ResolvedAt)
            .Ignore(dest => dest.ClosedAt)
            .Ignore(dest => dest.ResolutionNotes)
            .Ignore(dest => dest.AssignedToUserId)
            .Ignore(dest => dest.Customer)
            .Ignore(dest => dest.AssignedTo)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.Documents);

        // UpdateSupportTicketRequest to Entity
        config.NewConfig<UpdateSupportTicketRequest, SupportTicket>()
            .IgnoreNullValues(true)
            .Map(dest => dest.Status, src => src.Status != null ? Enum.Parse<TicketStatus>(src.Status) : TicketStatus.Open)
            .Map(dest => dest.Priority, src => src.Priority != null ? Enum.Parse<TicketPriority>(src.Priority) : TicketPriority.Low)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CustomerId)
            .Ignore(dest => dest.InstallationId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.AssignedAt)
            .Ignore(dest => dest.ResolvedAt)
            .Ignore(dest => dest.ClosedAt)
            .Ignore(dest => dest.Customer)
            .Ignore(dest => dest.AssignedTo)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.Documents);
    }
}