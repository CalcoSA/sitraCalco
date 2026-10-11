namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionLotDto
    {
        public long reception_id { get; set; }
        public long product_id { get; set; }
        public string lot_number { get; set; } = null!;
        public DateOnly? expiration_date { get; set; }
        public decimal? lot_size { get; set; }
        public decimal? unit_count { get; set; }
        public string? lot_status { get; set; }
        public string? shelf_life_comments { get; set; }
        public string? document_type { get; set; }
        public string? document_number { get; set; }
        public decimal? quantity { get; set; }
    }
}