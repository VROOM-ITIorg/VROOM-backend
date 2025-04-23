using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Services;
using VROOM.ViewModels;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("{controller}")]
    public class RiderController : Controller
    {
        private readonly AdminServices adminServices;
        private readonly UserManager<User> userManager;
        public RiderController(AdminServices _adminServices, UserManager<User> _userManager)
        {
            adminServices = _adminServices;
            userManager = _userManager;
        }

        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var owners = await adminServices.GetAllOwners();
            ViewData["AllOwners"] = owners;
            return View();
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create(AdminCreateRiderVM model)
        {
            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email is already Exist");
                return View(model);
            }
            if (ModelState.IsValid)
            {
                await adminServices.CreateNewRider(model);

                return RedirectToAction("GetAllRiders");
            }
            return View(model);
        }

        [HttpGet]
        [Route("GetAllRiders")]
        public IActionResult Index(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4, string sort = "name_asc", string owner= "All")
        {
            var res = adminServices.ShowAllRiders(status, Name, PhoneNumber, pageNumber, pageSize, sort, owner);
            var Riders = res.Result.Riders;
            var Owners = res.Result.owners;

            ViewData["Riders"] = Riders;
            ViewData["Owners"] = Owners;
            ViewData["Name"] = Name;
            ViewData["status"] = status;
            ViewData["sort"] = sort;
            ViewData["owner"] = owner;
            ViewData["pageSize"] = pageSize.ToString();

            return View("index");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var res = await adminServices.EditRider(id);
            var Rider = res.Rider;
            var owners = res.BusinessName;
            ViewData["AllOwners"] = owners;
            return View(Rider);
        }

        [HttpPost]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(AdminEditRiderVM model) 
        {
            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email is already Exist");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                  return View(model);
            }
            await adminServices.EditRider(model);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await adminServices.Delete(id);
            return RedirectToAction("Index");
        }
    }
}