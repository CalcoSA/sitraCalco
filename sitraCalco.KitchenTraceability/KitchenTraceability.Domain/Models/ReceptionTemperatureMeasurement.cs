namespace KitchenTraceability.Domain.Models
{
    public partial class ReceptionTemperatureMeasurement
    {
        public long temperature_measurement_id { get; set; }
        public long reception_lot_id { get; set; }
        public string measurement_point { get; set; } = null!;
        public decimal temperature_celsius { get; set; }
        public bool is_compliant { get; set; }
        public string measured_by { get; set; } = null!;
        public DateTime measured_at { get; set; }
        public string? comments { get; set; }
        public virtual ReceptionLot receptionLot { get; set; } = null!;
    }
}