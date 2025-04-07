using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ViewModels;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.ViewModels;

namespace API.Controllers
{
    public class RiderController : Controller
    {
        //private readonly VroomDbContext context;
        //private readonly RiderRepository riderManager;
        //public RiderController(VroomDbContext _context, RiderRepository _riderManager)
        //{
        //    context = _context;
        //    riderManager = _riderManager;
        //}


        //[HttpGet]
        //public IActionResult Create()
        //{
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create (RiderViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var newUser = new User
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            Name = model.UserName,
        //            Email = model.Email,
        //            UserName = model.UserName,
        //            NormalizedEmail = model.Email.ToUpper(),
        //            NormalizedUserName = model.Email.ToUpper(),
        //            EmailConfirmed = true,
        //            PasswordHash = new PasswordHasher<User>().HashPassword(null, "Default@123")
        //        };

        //        context.Users.Add(newUser);
        //        context.SaveChanges();

        //        string imagePath = null;
        //        if (model.ProfilePicture != null)
        //        {
        //            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Rider");
        //            Directory.CreateDirectory(uploadsFolder);

        //            var fileExt = Path.GetExtension(model.ProfilePicture.FileName);
        //            var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}{fileExt}";
        //            var filePath = Path.Combine(uploadsFolder, fileName);

        //            using (var fileStream = new FileStream(filePath, FileMode.Create))
        //            {
        //               model.ProfilePicture.CopyTo(fileStream);
        //            }

        //            model.ImagePath = $"/Images/Rider/{fileName}";
        //        }

        //        var newRider = new Rider();
        //        newRider.UserID = newUser.Id;
        //        newRider = model.ToModel();

        //        context.Riders.Add(newRider);
        //        context.SaveChanges();

        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.UserId = new SelectList(context.Users.ToList(), "Id", "Email");
        //    return View(model);
        //}



        //public IActionResult Index(string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        //{
        //    var Riders = riderManager.Search(Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize);
        //    ViewData["Riders"] = Riders;
        //    return View("index");
        //}




        //public IActionResult Edit(int id)
        //{
        //    var rider = context.Riders
        //        .Include(r => r.User)
        //        .FirstOrDefault(r => r.Id == id);

        //    if (rider == null)
        //    {
        //        return NotFound();
        //    }

        //    var viewModel = new RiderViewModel
        //    {
        //        Id = rider.Id,
        //        BusinessID = rider.BusinessID,
        //        UserID = rider.UserID,
        //        Status = rider.Status,
        //        Type = rider.Type,
        //        Vehicle = rider.Vehicle,
        //        Location = rider.Location,
        //        ExperienceLevel = rider.ExperienceLevel,
        //        UserName = rider.User?.UserName,
        //        Email = rider.User?.Email 
        //    };

        //    return View(viewModel);
        //}



        //[HttpPost]
        //public IActionResult Edit(RiderViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var rider = context.Riders.FirstOrDefault(r => r.Id == model.Id);
        //    if (rider == null)
        //    {
        //        return NotFound();
        //    }

        //    rider.BusinessID = model.BusinessID;
        //    rider.UserID = model.UserID;
        //    rider.Status = model.Status;
        //    rider.Type = model.Type;
        //    rider.Vehicle = model.Vehicle;
        //    rider.Location = model.Location;
        //    rider.ExperienceLevel = model.ExperienceLevel;
        //    rider.Rating = model.Rating;

        //    context.SaveChanges();
        //    return RedirectToAction("Index");
        //}




        //public IActionResult Delete(int id)
        //{
        //    var Rider = context.Riders.Where(i => i.Id == id).FirstOrDefault().User;

        //    context.Users.Remove(Rider);
        //    context.Riders.Remove(context.Riders.Where(i => i.Id == id).FirstOrDefault());

        //    context.SaveChanges();
        //    return RedirectToAction("Index");
        //}



    }
}
