namespace Inventory.Domain.Models
{
    public partial class InventoryExportParameter
    {
        public long export_parameter_id { get; set; }

        public string configuration_code { get; set; } = null!;
        public string configuration_name { get; set; } = null!;

        public string detail_record_type { get; set; } = null!;
        public string detail_record_subtype { get; set; } = null!;
        public string detail_record_version { get; set; } = null!;
        public string company_code { get; set; } = null!;

        public string reserved_field { get; set; } = null!;
        public string auxiliary_location { get; set; } = null!;
        public string lot_code { get; set; } = null!;
        public string label_number { get; set; } = null!;

        public string count_1_1 { get; set; } = null!;
        public string count_2_1 { get; set; } = null!;
        public string count_1_2 { get; set; } = null!;
        public string count_2_2 { get; set; } = null!;
        public string count_3_2 { get; set; } = null!;

        public string barcode { get; set; } = null!;
        public string detail_extension_1 { get; set; } = null!;
        public string detail_extension_2 { get; set; } = null!;

        public string header_record_type { get; set; } = null!;
        public string header_record_subtype { get; set; } = null!;
        public string header_record_version { get; set; } = null!;
        public string footer_record_type { get; set; } = null!;
        public string footer_record_subtype { get; set; } = null!;
        public string footer_record_version { get; set; } = null!;

        public bool is_active { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
