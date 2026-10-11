namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionLotEvaluationResultDto
    {
        public long reception_lot_id { get; set; }
        public string lot_status { get; set; } = null!;
        public IEnumerable<ReceptionTemperatureMeasurementResultDto> temperature_measurements { get; set; } = new List<ReceptionTemperatureMeasurementResultDto>();
        public ReceptionVisualInspectionResultDto? visual_inspection { get; set; }
        public long? nonconformity_id { get; set; }
        public string? nonconformity_number { get; set; }
        public string? nonconformity_form_code { get; set; }
        public string? nonconformity_reason { get; set; }
    }
}