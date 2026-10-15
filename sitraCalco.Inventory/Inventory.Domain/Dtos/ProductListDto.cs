namespace Inventory.Domain.Dtos
{
    public class ProductListDto
    {
        public long product_id { get; set; }
        public string product_name { get; set; } = null!;
        public string reference { get; set; } = null!;
        public string unit_of_measure { get; set; } = null!;
        public string? plan_id { get; set; }
        public string? image_path { get; set; }
        public string? ImageUrl { get; set; }
    }
}
