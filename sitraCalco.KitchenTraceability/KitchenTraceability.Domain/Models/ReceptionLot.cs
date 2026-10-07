namespace KitchenTraceability.Domain.Models
{
    public partial class ReceptionLot
    {
        public long reception_lot_id { get; set; }
        public long reception_id { get; set; }
        public string lot_number { get; set; } = null!;
        public string product_reference { get; set; } = null!;
        public DateOnly? expiration_date { get; set; }
        public decimal lot_size { get; set; }
        public string unit_of_measure { get; set; } = null!;
        public bool shelf_life { get; set; }
        public string lot_status { get; set; } = null!;
        public DateTime created_at { get; set; }
        public virtual Reception reception { get; set; } = null!;
        public virtual ICollection<ReceptionTemperatureMeasurement> receptionTemperatureMeasurements { get; set; } = new List<ReceptionTemperatureMeasurement>();
        public virtual ICollection<ReceptionVisualInspection> receptionVisualInspections { get; set; } = new List<ReceptionVisualInspection>();
    }
}