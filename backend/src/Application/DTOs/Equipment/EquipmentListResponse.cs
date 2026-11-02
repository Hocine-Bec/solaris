namespace Application.DTOs.Equipment;

public class EquipmentListResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? InstallationName { get; set; }
    public bool IsUnderWarranty { get; set; }
}