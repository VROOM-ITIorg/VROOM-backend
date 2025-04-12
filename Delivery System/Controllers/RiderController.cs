using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ViewModels;
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
        public RiderController(VroomDbContext _context, RiderRepository _riderManager, BusinessOwnerRepository _ownerRepository)
        {
            context = _context;
            riderManager = _riderManager;
            ownerRepository = _ownerRepository;
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
        public IActionResult Create(AdminCreateRiderVM model)
        {
            if (ModelState.IsValid)
            {
                var businessOwnerId = context.BusinessOwners.Where(i => i.User.Name == model.BusinessName).FirstOrDefault().UserID;

                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.UserName,
                    Email = model.Email,
                    UserName = model.UserName,
                    NormalizedEmail = model.Email.ToUpper(),
                    NormalizedUserName = model.Email.ToUpper(),
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "Default@123"),
                    PhoneNumber = model.PhoneNumber
                };

                context.Users.Add(newUser);
                context.SaveChanges();

                string imagePath = null;
                if (model.ProfilePicture != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Rider");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileExt = Path.GetExtension(model.ProfilePicture.FileName);
                    var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}{fileExt}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        model.ProfilePicture.CopyTo(fileStream);
                    }

                    model.ImagePath = $"/Images/Rider/{fileName}";
                }

                var newRider = new Rider();
                newRider = model.ToModel();
                newRider.UserID = newUser.Id;
                newRider.BusinessID = businessOwnerId;
                newUser.ProfilePicture = model.ImagePath;
                context.Riders.Add(newRider);
                context.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(context.Users.ToList(), "Id", "Email");
            return View(model);
        }


        [Route("Index")]
        public IActionResult Index(int status = -1 ,string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {
            var Riders = riderManager.Search( status : status ,Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize);
            

            ViewData["Riders"] = Riders;

            return View("index");
        }




        public IActionResult Edit(string id)
        {
            var rider = context.Riders
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserID == id);

            if (rider == null)
            {
                return NotFound();
            }

            var viewModel = new AdminCreateRiderVM
            {
                Status = rider.Status,
                VehicleType = rider.VehicleType,
                Location = rider.Area,
                ExperienceLevel = rider.ExperienceLevel,
                UserName = rider.User?.UserName,
                Email = rider.User?.Email
            };

            return View(viewModel);
        }



        [HttpPost]
        //public IActionResult Edit(AdminCreateRiderVM model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var rider = context.Riders.FirstOrDefault(r => r.UserID == model.UserID);
        //    if (rider == null)
        //    {
        //        return NotFound();
        //    }

        //    rider.BusinessID = model.BusinessID;
        //    rider.UserID = model.UserID;
        //    rider.Status = model.Status;
        //    rider.VehicleType = model.VehicleType;
        //    rider.Area = model.Location;
        //    rider.ExperienceLevel = model.ExperienceLevel;
        //    rider.Rating = 0;

        //    context.SaveChanges();
        //    return RedirectToAction("Index");
        //}




        public IActionResult Delete(string id)
        {
            var Rider = context.Riders.Where(i => i.UserID == id).FirstOrDefault().User;

            context.Users.Remove(Rider);
            context.Riders.Remove(context.Riders.Where(i => i.UserID == id).FirstOrDefault());

            context.SaveChanges();
            return RedirectToAction("Index");
        }



    }
}
