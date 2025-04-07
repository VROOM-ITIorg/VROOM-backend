using System.ComponentModel.DataAnnotations;


    public class UserLoginVM
    {
        [Required(ErrorMessage = "This Field is Required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Value Must at least 6 letter ")]
        public string Method { get; set; }



        [Required(ErrorMessage = "This Field is Required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Value Must at least 8 letter ")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

