using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderController : ControllerBase
    {

        private readonly VroomDbContext  context;
        private readonly RiderRepository riderManager;
        private readonly RiderService ridService;

        private readonly BusinessOwnerService _businessOwnerService;
        public RiderController(VroomDbContext _context, RiderRepository _riderManager, BusinessOwnerService businessOwnerService, RiderService _riderService)
        {
            _context = context;
            _riderManager = riderManager;
            _businessOwnerService = businessOwnerService;
            _riderService = ridService
        }



        [HttpGet("assigned/{orderId}")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> ViewAssignedOrder(int orderId)
        {
            var result = await _businessOwnerService.ViewAssignedOrderAsync(orderId);

            if (result == null)
                return NotFound(new { message = "Order not found or not assigned to you." });

            return Ok(result);
        }




        [HttpPost("start-delivery")]
        public async Task<IActionResult> StartDelivery([FromQuery] string riderId, [FromQuery] int orderId)
        {
            var result = await ridService.StartDeliveryAsync(riderId, orderId);
            if (result == null)
                return BadRequest("Unable to start delivery. Check rider status or order state.");

            return Ok(new
            {
                Message = "Delivery started successfully.",
                Order = result
            });
        }

        [HttpPost("update-delivery-status")]
        public async Task<IActionResult> UpdateDeliveryStatus([FromQuery] string riderId, [FromQuery] int orderId, [FromQuery] OrderStateEnum newState)
        {
            if (string.IsNullOrEmpty(riderId) || orderId <= 0)
                return BadRequest(new { Message = "Invalid rider ID or order ID." });

            try
            {
                var updatedOrder = await ridService.UpdateDeliveryStatusAsync(riderId, orderId, newState);

                return Ok(new
                {
                    Message = $"Order delivery status successfully updated to {newState}.",
                    Order = updatedOrder
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new {ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { ex.Message });
            }
        }



        [HttpGet]
        //public IActionResult Create()
        //{
        //    ViewBag.UserId = new SelectList(_context.Users.ToList(), "Id", "Email");
        //    return View();
        //}

        //[HttpPost]
        //public IActionResult Create(RiderViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var newUser = new User
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            Name = model.Name,
        //            Email = model.Email,
        //            UserName = model.Name,
        //            NormalizedEmail = model.Email.ToUpper(),
        //            NormalizedUserName = model.Email.ToUpper(),
        //            EmailConfirmed = true,
        //            Role = "Rider",
        //            PasswordHash = new PasswordHasher<User>().HashPassword(null, "Default@123")
        //        };

        //        _context.Users.Add(newUser);
        //        _context.SaveChanges();

        //        var FileExt =  Path.GetExtension(model.ImagePath.FileName);

        //        var FileName = $"{DateTime.Now:yyyyMMddHHmmssfff}{FileExt}";

        //        FileStream fileStream = new FileStream(
        //               Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Rider", FileName),
        //        FileMode.Create);

        //        model.ImagePath.CopyTo(fileStream);

        //        fileStream.Position = 0;

        //        //save path to database;
        //        model.Path = $"/Images/Rider/{FileName}";

        //        var newRider = new Rider
        //        {
        //            PhoneNumber = model.PhoneNumber,
        //            ImagePath = model.Path,
        //            UserId = newUser.Id
        //        };

        //        _context.Riders.Add(newRider);
        //        _context.SaveChanges();

        //        return RedirectToAction("Index");
        //    }

        //    return View(model);
        //}


        [Route("index")]

        public void Index(string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {
            //var Riders = riderManager.Search(Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize);
            //return Ok(Riders);

        }




        //public IActionResult Edit(int id)
        //{
        //    var Rider = _context.Riders.Where(i => i.Id == id).ToList().FirstOrDefault();
        //    return View(Rider);
        //}


        //[HttpPost]
        //public IActionResult Edit(int id, RiderViewModel Rider)
        //{

        //    var SelectedRider = _context.Riders.Where(i => i.Id == id).FirstOrDefault();
        //    SelectedRider.User.Name = Rider.Name;
        //    SelectedRider.PhoneNumber = Rider.ToModel().PhoneNumber;
        //    SelectedRider.ImagePath = Rider.ToModel().ImagePath;
        //    _context.SaveChanges();
        //    return RedirectToAction("Index");
        //}



        //public IActionResult Delete(int id)
        //{
        //    var Rider = _context.Riders.Where(i => i.Id == id).FirstOrDefault().User;

        //    _context.Users.Remove(Rider);
        //    _context.Riders.Remove(_context.Riders.Where(i => i.Id == id).FirstOrDefault());

        //    _context.SaveChanges();
        //    return RedirectToAction("Index");
        //}



    }
}
