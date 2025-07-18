namespace VROOM.ViewModels
{
    // ViewModel For Geting The Customers for Business Owner in the order creation
    public record CustomerVM
    {
        public string UserID { get; init; }
        public string Name { get; init; }
        public string Email { get; init; }

        public string PhoneNumber { get; set; }
        public LocationDto? Location { get; init; }


    }

    public class CustomerRegisterRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string ProfilePicture { get; set; }
        public LocationDto Location { get; set; }
    }
}

