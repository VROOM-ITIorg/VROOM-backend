using Microsoft.AspNetCore.Authorization;
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
        public RiderController(AdminServices _adminServices)
        {
            adminServices = _adminServices;
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
            if (ModelState.IsValid)
            {
                await adminServices.CreateNewRider(model);
                return RedirectToAction("Index");
            }
            return View(model);
        }


        [Route("Index")]
        public IActionResult Index(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {
            var Riders = adminServices.ShowAllRiders(status, Name, PhoneNumber, pageNumber, pageSize);

            ViewData["Riders"] = Riders;

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