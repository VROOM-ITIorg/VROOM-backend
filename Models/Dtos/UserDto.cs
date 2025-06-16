namespace VROOM.Models.Dtos
{
    public class UserDto
    {
        public string Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? ProfilePicture { get; set; }
        public Address? Address { get; set; }
        public string Role { get; set; }
    }
}