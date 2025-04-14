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
    [Route("vroom-admin/{controller}")]
    public class RiderController : Controller
    {
        private readonly VroomDbContext context;
        private readonly RiderRepository riderManager;
        private readonly BusinessOwnerRepository ownerRepository;
        private readonly AdminServices adminServices;
        public RiderController(VroomDbContext _context, RiderRepository _riderManager, BusinessOwnerRepository _ownerRepository, AdminServices _adminServices)
        {
            context = _context;
            riderManager = _riderManager;
            ownerRepository = _ownerRepository;
            adminServices = _adminServices;
        }


        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewData["AllOwners"] = await ownerRepository.GetAllAsync();
            return View();
        }

        [HttpPost]
        [Route("create")]
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
            var Riders = riderManager.Search(status: status, Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize);

            ViewData["Riders"] = Riders;

            return View("index");
        }


        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var rider = context.Riders
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserID == id);

            if (rider == null)
            {
                return NotFound();
            }

            var viewModel = new AdminEditRiderVM
            {
                UserID = rider.UserID,
                Status = rider.Status,
                VehicleType = rider.VehicleType,
                Location = rider.Area,
                ExperienceLevel = rider.ExperienceLevel,
                UserName = rider.User?.UserName,
                Email = rider.User?.Email,
                PhoneNumber = rider.User.PhoneNumber,
                ImagePath = rider.User.ProfilePicture
            };

            ViewData["AllOwners"] = await ownerRepository.GetAllAsync();


            return View(viewModel);
        }



        [HttpPost]
        [Route("Edit/{id}")]

        public IActionResult Edit(AdminEditRiderVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var rider = context.Riders.FirstOrDefault(r => r.UserID == model.UserID);
            if (rider == null)
            {
                return NotFound();
            }

            rider.UserID = model.UserID;
            rider.Status = model.Status;
            rider.VehicleType = model.VehicleType;
            rider.Area = model.Location;
            rider.ExperienceLevel = model.ExperienceLevel;
            rider.Rating = 0;
            rider.User.Name = model.UserName;
            

            context.SaveChanges();
            return RedirectToAction("Index");
        }


        [HttpGet]
        [Route("Delete/{id}")]
        public IActionResult Delete(string id)
        {
            var Rider = context.Riders.Where(i => i.UserID == id).FirstOrDefault();

            Rider.User.IsDeleted = true;            
            context.SaveChanges();
            return RedirectToAction("Index");
        }



    }
}