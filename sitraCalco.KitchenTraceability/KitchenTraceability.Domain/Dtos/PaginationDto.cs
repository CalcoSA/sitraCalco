namespace KitchenTraceability.Domain.Dtos
{
    public class PaginationDto
    {
        public int Page { get; set; } = 1;
        public int Take { get; set; } = 10;
    }
}