using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Services;
using VROOM.ViewModels;

namespace Delivery_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BusinessOwnerController : Controller
    {

        private readonly AdminServices adminServices;
        private readonly UserManager<User> userManager;

        public BusinessOwnerController(AdminServices _adminServices, UserManager<User> _userManager)
        {
            adminServices = _adminServices;
            userManager = _userManager;
        }


        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewData["AllOwners"] = await adminServices.GetAllOwners();
            return View();
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(AdminCreateBusOnwerVM model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already Exist");
                    return View(model);
                }

                await adminServices.CreateNewOwner(model);

                return RedirectToAction("GetAllOwners");
            }
            return View(model);
        }

        [HttpGet]
        [Route("GetAllOwners")]
        public IActionResult GetAllOwners(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4, string sort = "name_asc")
        {
            var Owners = adminServices.ShowAllOwners( Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize, sort : sort);

            ViewData["Name"] = Name;
            ViewData["status"] = status;
            ViewData["sort"] = sort;
            ViewData["pageSize"] = pageSize.ToString();

            ViewData["Owners"] = Owners;
            return View();
        }


        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var viewModel = adminServices.EditOwner(id);
            return View(viewModel);
        }



        [HttpPost]
        [Route("Edit/{id}")]

        public async Task<IActionResult> Edit(AdminEditBusOwnerVM model)
        {
            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email is already Exist");
                return View(model);
            }
            await adminServices.EditOwner(model);
            return RedirectToAction("GetAllOwners");
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
