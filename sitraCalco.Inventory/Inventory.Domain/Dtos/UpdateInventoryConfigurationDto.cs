using System.Text.Json.Serialization;

namespace Inventory.Domain.Dtos;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateInventoryConfigurationDto
{
    private string? _name;
    private int? _startDay;
    private int? _endDay;

    public string? InventoryConfigurationName
    {
        get => _name;
        set { _name = value; HasName = true; }
    }

    public int? StartDay
    {
        get => _startDay;
        set { _startDay = value; HasStartDay = true; }
    }

    public int? EndDay
    {
        get => _endDay;
        set { _endDay = value; HasEndDay = true; }
    }

    // Distinguir campos omitidos de null explícito en una actualización parcial.
    [JsonIgnore] public bool HasName { get; private set; }
    [JsonIgnore] public bool HasStartDay { get; private set; }
    [JsonIgnore] public bool HasEndDay { get; private set; }
}
