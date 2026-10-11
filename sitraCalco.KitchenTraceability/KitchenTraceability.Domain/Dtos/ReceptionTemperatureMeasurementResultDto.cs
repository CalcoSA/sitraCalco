namespace KitchenTraceability.Domain.Dtos
{
    public class ReceptionTemperatureMeasurementResultDto
    {
        public long temperature_measurement_id { get; set; }
        public string measurement_point { get; set; } = null!;
        public decimal temperature_celsius { get; set; }
        public string temperature_status { get; set; } = null!;
        public string? comments { get; set; }
    }
}