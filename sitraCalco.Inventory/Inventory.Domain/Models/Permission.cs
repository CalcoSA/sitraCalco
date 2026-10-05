namespace Inventory.Domain.Models
{
    public partial class Permission
    {
        public long permission_id { get; set; }

        public string permission_key { get; set; } = null!;

        public string permission_value { get; set; } = null!;
    }
}
