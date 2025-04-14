using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VROOM.Repositories;
using VROOM.Services;
using ViewModels.Account;
using VROOM.Models;

namespace Delivery_System.Controllers
{
    public class AccountController : Controller
    {
        private AdminServices adminService;
        public AccountController(AdminServices _adminService)
        {
            adminService = _adminService;
        }

        //[Route("login")]
        [HttpGet]
        public IActionResult login()
        {
            return View();
        }


        [Route("login")]
        [HttpPost]
        public async Task<IActionResult> loginAsync(LoginViewModel admin)
        {
            if (ModelState.IsValid)
            {
                var res = await adminService.Login(admin);
                if (res.Succeeded)
                {
                    var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
                    if (role == "Admin")
                    return RedirectToAction(controllerName:"order", actionName: "ActiveOrder");

                }
                else if (res.IsLockedOut || res.IsNotAllowed)
                {
                    ModelState.AddModelError("", "Sorry try again Later!!!!");
                }
                else
                {
                    ModelState.AddModelError("", "Sorry Invalid Email Or User Name Or Password");
                }
            }
            return View(admin);

        }


    }
}
