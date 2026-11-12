namespace KitchenTraceability.Domain.Models
{
    public partial class ReceptionTransportEvaluation
    {
        public long transport_evaluation_id { get; set; }
        public long reception_id { get; set; }
        public bool is_compliant { get; set; }
        public string? comments { get; set; }
        public string evaluated_by { get; set; } = null!;
        public DateTime evaluated_at { get; set; }
        public virtual Reception reception { get; set; } = null!;
    }
}