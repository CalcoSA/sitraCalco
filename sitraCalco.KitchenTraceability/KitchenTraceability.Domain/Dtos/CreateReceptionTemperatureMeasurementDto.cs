namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionTemperatureMeasurementDto
    {
        public string measurement_point { get; set; } = null!;
        public decimal temperature_celsius { get; set; }
        public string? comments { get; set; }
    }
}