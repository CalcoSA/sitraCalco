namespace Inventory.Domain.Dtos
{
    public class PermissionDto
    {
        public long PermissionId { get; set; }

        public string PermissionKey { get; set; } = null!;

        public string PermissionValue { get; set; } = null!;
    }
}
