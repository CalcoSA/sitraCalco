namespace Inventory.Domain.Models
{
    public partial class InventoryLog
    {
        public long log_id { get; set; }

        public string action { get; set; } = null!;

        public string module { get; set; } = null!;

        public string description { get; set; } = null!;

        public string user_name { get; set; } = null!;

        public DateTime created_at { get; set; }
    }
}