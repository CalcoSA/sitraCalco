namespace Authentication.Domain.Dtos
{
    public class WordpressUserDto
    {
        public ulong WordpressUserId { get; set; }
        public string? WordpressUserLogin { get; set; }
        public string? WordpressDisplayName { get; set; }
        public string? WordpressUserPass { get; set; }
    }
}