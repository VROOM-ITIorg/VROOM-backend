using Microsoft.AspNetCore.Authorization;
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
        public BusinessOwnerController(AdminServices _adminServices)
        {
            adminServices = _adminServices;
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
