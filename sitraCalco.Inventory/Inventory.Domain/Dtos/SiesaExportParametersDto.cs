namespace Inventory.Domain.Dtos
{
    public class SiesaExportParametersDto
    {
        public string DetailRecordType { get; set; } = null!;
        public string DetailRecordSubtype { get; set; } = null!;
        public string DetailRecordVersion { get; set; } = null!;
        public string CompanyCode { get; set; } = null!;
        public string ReservedField { get; set; } = null!;
        public string AuxiliaryLocation { get; set; } = null!;
        public string LotCode { get; set; } = null!;
        public string LabelNumber { get; set; } = null!;
        public string Count_1_1 { get; set; } = null!;
        public string Count_2_1 { get; set; } = null!;
        public string Count_1_2 { get; set; } = null!;
        public string Count_2_2 { get; set; } = null!;
        public string Count_3_2 { get; set; } = null!;
        public string Barcode { get; set; } = null!;
        public string DetailExtension1 { get; set; } = null!;
        public string DetailExtension2 { get; set; } = null!;
        public string HeaderRecordType { get; set; } = null!;
        public string HeaderRecordSubtype { get; set; } = null!;
        public string HeaderRecordVersion { get; set; } = null!;
        public string FooterRecordType { get; set; } = null!;
        public string FooterRecordSubtype { get; set; } = null!;
        public string FooterRecordVersion { get; set; } = null!;
    }
}
