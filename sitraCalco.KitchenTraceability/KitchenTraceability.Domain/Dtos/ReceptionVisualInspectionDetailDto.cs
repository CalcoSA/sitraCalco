namespace KitchenTraceability.Domain.Dtos
{
    public class ReceptionVisualInspectionDetailDto
    {
        public decimal compliant_units { get; set; }
        public decimal non_compliant_units { get; set; }
        public string? comments { get; set; }
    }
}