namespace KitchenTraceability.Domain.Models
{
    public partial class ReceptionVisualInspection
    {
        public long visual_inspection_id { get; set; }
        public long reception_lot_id { get; set; }
        public decimal inspected_units { get; set; }
        public decimal compliant_units { get; set; }
        public decimal non_compliant_units { get; set; }
        public string? comments { get; set; }
        public string evaluated_by { get; set; } = null!;
        public DateTime evaluated_at { get; set; }
        public virtual ReceptionLot receptionLot { get; set; } = null!;
    }
}