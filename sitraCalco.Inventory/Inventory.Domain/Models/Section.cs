namespace Inventory.Domain.Models
{
    public partial class Section
    {
        public long section_id { get; set; }

        public string section_name { get; set; } = null!;

        public bool is_active { get; set; }
    }
}