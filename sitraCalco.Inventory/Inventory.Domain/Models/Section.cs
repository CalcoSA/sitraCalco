namespace Inventory.Domain.Models
{
    public partial class Section
    {
        public long section_id { get; set; }

        public long solution_center_type_id { get; set; }

        public SolutionCenterType SolutionCenterType { get; set; } = null!;

        public string section_name { get; set; } = null!;

        public bool is_active { get; set; }
    }
}
