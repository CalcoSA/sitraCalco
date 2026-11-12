namespace KitchenTraceability.Domain.Dtos
{
    public class ReceptionVisualInspectionResultDto
    {
        public long visual_inspection_id { get; set; }
        public decimal inspected_units { get; set; }
        public decimal compliant_units { get; set; }
        public decimal non_compliant_units { get; set; }
        public string? comments { get; set; }
    }
}