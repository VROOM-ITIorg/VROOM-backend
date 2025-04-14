using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter an email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }



//public class CustomAuthorizeAttribute : AuthorizeAttribute
//    {
//        public override void OnAuthorization(AuthorizationContext filterContext)
//        {
//            base.OnAuthorization(filterContext);

//            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
//            {
//                filterContext.Result = new RedirectResult("~/Account/Login");
//            }
//        }
//    }



}
