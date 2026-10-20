namespace Inventory.Domain.Dtos;

public class InventoryConfigurationDto
{
    public long InventoryConfigurationId { get; set; }
    public string InventoryConfigurationName { get; set; } = null!;
    public int StartDay { get; set; }
    public int EndDay { get; set; }
}

public class InventoryConfigurationListDto : InventoryConfigurationDto
{
    public List<InventoryConfigurationDayDto> DaysOfWeek { get; set; } = new();
}
