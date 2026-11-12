namespace Inventory.Domain.Dtos;

public class InventoryConfigurationDayDto
{
    public long InventoryConfigurationDayId { get; set; }
    public string DayOfWeek { get; set; } = null!;
}
