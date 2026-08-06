namespace Authentication.Domain.Dtos
{
    public class AuthUserDto
    {
        public string TokenType { get; set; } = "Bearer";
        public string? AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDto? User { get; set; }
        public IEnumerable<MenuOptionDto> MenuOptions { get; set; } = new List<MenuOptionDto>();
    }
}