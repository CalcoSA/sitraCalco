namespace KitchenTraceability.Domain.Models
{
    public partial class ReceptionLot
    {
        public long reception_lot_id { get; set; }
        public long reception_id { get; set; }
        public string lot_number { get; set; } = null!;
        public string product_reference { get; set; } = null!;
        public DateOnly? expiration_date { get; set; }
        public decimal? lot_size { get; set; }
        public string? lot_size_unit { get; set; }
        public decimal? unit_count { get; set; }
        public string unit_of_measure { get; set; } = null!;
        public bool? shelf_life { get; set; }
        public string? shelf_life_comments { get; set; }
        public string lot_status { get; set; } = null!;
        public DateTime created_at { get; set; }
        public virtual Reception reception { get; set; } = null!;
        public virtual ICollection<Conformity> conformities { get; set; } = new List<Conformity>();
        public virtual ICollection<ReceptionTemperatureMeasurement> receptionTemperatureMeasurements { get; set; } = new List<ReceptionTemperatureMeasurement>();
        public virtual ReceptionVisualInspection? receptionVisualInspection { get; set; }
    }
}