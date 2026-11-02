namespace Application.DTOs.Equipment;

public class EquipmentInventoryResponse
{
    public string Type { get; set; } = string.Empty;
    public int InStock { get; set; }
    public int Assigned { get; set; }
    public int Installed { get; set; }
    public int NeedsRepair { get; set; }
    public int Total { get; set; }
}