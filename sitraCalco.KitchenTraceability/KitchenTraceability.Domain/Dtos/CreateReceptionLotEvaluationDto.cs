namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionLotEvaluationDto
    {
        public long reception_lot_id { get; set; }
        public IEnumerable<CreateReceptionTemperatureMeasurementDto>? temperature_measurements { get; set; }
        public CreateReceptionVisualInspectionDto? visual_inspection { get; set; }
        public string? document_type { get; set; }
        public string? document_number { get; set; }
    }
}