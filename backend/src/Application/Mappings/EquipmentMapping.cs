using Domain.Entities;
using Domain.Enums;
using Mapster;
using Application.DTOs.Equipment;

namespace Application.Mappings;

public class EquipmentMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to EquipmentResponse
        config.NewConfig<Equipment, EquipmentResponse>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.InstallationName, src => src.Installation != null ? src.Installation.ProjectName : null)
            .Map(dest => dest.IsUnderWarranty, src => src.WarrantyExpiryDate.HasValue && src.WarrantyExpiryDate.Value > DateTime.UtcNow);

        // Entity to EquipmentListResponse
        config.NewConfig<Equipment, EquipmentListResponse>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.InstallationName, src => src.Installation != null ? src.Installation.ProjectName : null)
            .Map(dest => dest.IsUnderWarranty, src => src.WarrantyExpiryDate.HasValue && src.WarrantyExpiryDate.Value > DateTime.UtcNow);

        // CreateEquipmentRequest to Entity
        config.NewConfig<CreateEquipmentRequest, Equipment>()
            .Map(dest => dest.Type, src => Enum.Parse<EquipmentType>(src.Type))
            .Map(dest => dest.Status, src => EquipmentStatus.InStock)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.InstallationId)
            .Ignore(dest => dest.Installation);

        // UpdateEquipmentRequest to Entity
        config.NewConfig<UpdateEquipmentRequest, Equipment>()
            .IgnoreNullValues(true)
            .Map(dest => dest.Status, src => src.Status != null ? Enum.Parse<EquipmentStatus>(src.Status) : EquipmentStatus.InStock)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Type)
            .Ignore(dest => dest.Model)
            .Ignore(dest => dest.SerialNumber)
            .Ignore(dest => dest.Manufacturer)
            .Ignore(dest => dest.PurchaseDate)
            .Ignore(dest => dest.Cost)
            .Ignore(dest => dest.Installation);
    }
}