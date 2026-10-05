namespace Inventory.Domain.Models
{
    public partial class SolutionCenterSection
    {
        public long solution_center_section_id { get; set; }

        public long solution_center_id { get; set; }

        public long section_id { get; set; }

        public bool is_active { get; set; }
    }
}
