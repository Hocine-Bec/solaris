namespace Application.DTOs.Equipment;

public class UpdateEquipmentRequest
{
    public string? Status { get; set; }
    public int? InstallationId { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? Specifications { get; set; }
}