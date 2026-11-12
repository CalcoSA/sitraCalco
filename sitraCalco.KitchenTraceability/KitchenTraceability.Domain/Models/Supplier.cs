namespace KitchenTraceability.Domain.Models
{
    public partial class Supplier
    {
        public long supplier_id { get; set; }
        public string supplier_code { get; set; } = null!;
        public string name { get; set; } = null!;
        public virtual ICollection<Reception> receptions { get; set; } = new List<Reception>();
    }
}