namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionResultDto
    {
        public long reception_id { get; set; }
        public string reception_number { get; set; } = null!;
        public string status { get; set; } = null!;
    }
}