namespace Authentication.Domain.Dtos
{
    public class UpdateUserDto
    {
        public int IdUser { get; set; }
        public bool StatusUser { get; set; }
        public int IdRole { get; set; }
    }
}