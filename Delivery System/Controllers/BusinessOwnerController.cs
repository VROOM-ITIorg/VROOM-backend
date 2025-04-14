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
    [Route("vroom-admin/{controller}")]
    public class BusinessOwnerController : Controller
    {
        private readonly VroomDbContext context;
        private readonly BusinessOwnerRepository ownerRepository;
        private readonly AdminServices adminServices;
        private readonly BusinessOwnerService businessOwnerService;
        public BusinessOwnerController(VroomDbContext _context,BusinessOwnerRepository _ownerRepository, AdminServices _adminServices, BusinessOwnerService _businessOwnerService)
        {
            context = _context;
            ownerRepository = _ownerRepository;
            adminServices = _adminServices;
            businessOwnerService = _businessOwnerService;
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
        public IActionResult GetAllOwners(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {
            var Owners = ownerRepository.Search( Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize);


            ViewData["Owners"] = Owners;

            return View();
        }


        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var owner = context.BusinessOwners
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserID == id);

            if (owner == null)
            {
                return NotFound();
            }

            var viewModel = new AdminEditBusOwnerVM
            {
                UserID = owner.UserID,
                OwnerName = owner.User.Name,
                BusinessName = owner.BusinessType,
                Email = owner.User?.Email,
                PhoneNumber = owner.User.PhoneNumber,
                ImagePath = owner.User.ProfilePicture,
                Address = owner.User.Address?.Area

            };



            return View(viewModel);
        }



        [HttpPost]
        [Route("Edit/{id}")]

        public IActionResult Edit(AdminEditBusOwnerVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var owner = context.BusinessOwners.FirstOrDefault(r => r.UserID == model.UserID);
            if (owner == null)
            {
                return NotFound();
            }

            owner.UserID = model.UserID;
            owner.BusinessType = model.BusinessName;
            owner.User.Address.Area = model.Address;
            owner.User.Name = model.OwnerName;
            owner.User.Email = model.Email;
            owner.User.ProfilePicture = adminServices.UploadImageProfile<AdminEditBusOwnerVM>(model);
            context.SaveChanges();
            return RedirectToAction("GetAllOwners");
        }


        [HttpGet]
        [Route("Delete/{id}")]
        public IActionResult Delete(string id)
        {
            var Owner = context.BusinessOwners.Where(i => i.UserID == id).FirstOrDefault();

            //var User = context.BusinessOwners.Where(i => i.UserID == id).FirstOrDefault().User;
            //context.BusinessOwners.Remove(context.BusinessOwners.Where(i => i.UserID == id).FirstOrDefault());
            //context.Users.Remove(context.Users.Where(i => i.Id == id).FirstOrDefault());

            Owner.User.IsDeleted = true;
            context.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
