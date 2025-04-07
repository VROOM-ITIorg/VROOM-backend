using VROOM.Models;

public static class AccountExt
    {
        public static User ToModel(this UserRegisterVM viewmodel)
        {
            return new User
            {
                UserName = viewmodel.UserName,
                Email = viewmodel.Email,
                PhoneNumber = viewmodel.PhoneNumber,
            };
        }
    }

