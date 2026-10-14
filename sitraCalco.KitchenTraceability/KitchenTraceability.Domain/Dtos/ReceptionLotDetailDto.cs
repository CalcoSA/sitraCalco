namespace KitchenTraceability.Domain.Dtos
{
    public class ReceptionLotDetailDto
    {
        public long reception_lot_id { get; set; }
        public string lot_number { get; set; } = null!;
        public string product_reference { get; set; } = null!;
        public string? product_name { get; set; }
        public string unit_of_measure { get; set; } = null!;
        public string? guide_number { get; set; }
        public DateOnly? expiration_date { get; set; }
        public decimal? lot_size { get; set; }
        public string? lot_size_unit { get; set; }
        public decimal? unit_count { get; set; }
        public bool? shelf_life { get; set; }
        public string? shelf_life_comments { get; set; }
        public string lot_status { get; set; } = null!;
        public ReceptionVisualInspectionDetailDto? visual_inspection { get; set; }
        public IEnumerable<ReceptionTemperatureMeasurementResultDto> temperature_measurements { get; set; } = new List<ReceptionTemperatureMeasurementResultDto>();
        public IEnumerable<ConformityDetailDto> nonconformities { get; set; } = new List<ConformityDetailDto>();
    }
}