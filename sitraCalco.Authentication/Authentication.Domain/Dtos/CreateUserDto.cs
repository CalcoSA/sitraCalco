namespace Authentication.Domain.Dtos
{
    public class CreateUserDto
    {
        public ulong WordpressUserId { get; set; }
        public string? UserLogin { get; set; }
        public string? UserName { get; set; }
        public bool StatusUser { get; set; }
        public int IdRole { get; set; }
    }
}