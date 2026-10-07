namespace KitchenTraceability.Domain.Models
{
    public partial class Reception
    {
        public long reception_id { get; set; }
        public string reception_number { get; set; } = null!;
        public long reception_type_id { get; set; }
        public long supplier_id { get; set; }
        public string? guide_number { get; set; }
        public DateTime reception_date { get; set; }
        public string operator_user { get; set; } = null!;
        public string? comments { get; set; }
        public DateTime created_at { get; set; }
        public virtual ReceptionType receptionType { get; set; } = null!;
        public virtual ICollection<ReceptionLot> receptionLots { get; set; } = new List<ReceptionLot>();
        public virtual ICollection<ReceptionTransportEvaluation> receptionTransportEvaluations { get; set; } = new List<ReceptionTransportEvaluation>();
        public virtual Supplier supplier { get; set; } = null!;
    }
}