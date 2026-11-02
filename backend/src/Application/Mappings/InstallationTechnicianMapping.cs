using Application.DTOs.InstallationTechnician;
using Domain.Entities;
using Mapster;

namespace Application.Mappings;

public class InstallationTechnicianMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to TechnicianAssignmentResponse
        config.NewConfig<InstallationTechnician, TechnicianAssignmentResponse>()
            .Map(dest => dest.TechnicianName, src => $"{src.Technician.FirstName} {src.Technician.LastName}")
            .Map(dest => dest.IsCompleted, src => src.CompletedDate.HasValue);

        // CreateTechnicianAssignmentRequest to Entity
        config.NewConfig<CreateTechnicianAssignmentRequest, InstallationTechnician>()
            .Map(dest => dest.AssignedDate, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CompletedDate)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.Technician);

        // UpdateTechnicianAssignmentRequest to Entity
        config.NewConfig<UpdateTechnicianAssignmentRequest, InstallationTechnician>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.InstallationId)
            .Ignore(dest => dest.TechnicianId)
            .Ignore(dest => dest.AssignedDate)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.Technician);
    }
}