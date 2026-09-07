namespace Inventory.Domain.Models
{
    public partial class SolutionCenter
    {
        public long solution_center_id { get; set; }

        public long solution_center_type_id { get; set; }

        public string solution_center_code { get; set; } = null!;

        public string solution_center_name { get; set; } = null!;

        public bool is_active { get; set; }
    }
}