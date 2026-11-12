namespace KitchenTraceability.Domain.Dtos
{
    public class ReceptionDetailDto
    {
        public long reception_id { get; set; }
        public string reception_number { get; set; } = null!;
        public string reception_type_name { get; set; } = null!;
        public string? reception_type_form_code { get; set; }
        public string supplier_code { get; set; } = null!;
        public string supplier_name { get; set; } = null!;
        public DateTime reception_date { get; set; }
        public string status { get; set; } = null!;
        public bool? transport_is_compliant { get; set; }
        public string? transport_comments { get; set; }
        public IEnumerable<ReceptionLotDetailDto> lots { get; set; } = new List<ReceptionLotDetailDto>();
        public IEnumerable<ConformityDetailDto> nonconformities { get; set; } = new List<ConformityDetailDto>();
    }
}