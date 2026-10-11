namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionLotResultDto
    {
        public long reception_lot_id { get; set; }
        public string lot_status { get; set; } = null!;
        public bool? shelf_life { get; set; }
        public bool requires_temperature { get; set; }
        public bool requires_visual_inspection { get; set; }
        public long? nonconformity_id { get; set; }
        public string? nonconformity_number { get; set; }
        public string? nonconformity_form_code { get; set; }
    }
}