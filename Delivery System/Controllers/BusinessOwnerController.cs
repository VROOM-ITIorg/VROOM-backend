using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VROOM.Data;
using VROOM.Repositories;

namespace Delivery_System.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("vroom-admin/{controller}")]
    public class BusinessOwnerController : Controller
    {
        private readonly VroomDbContext context;
        private readonly BusinessOwnerRepository businessOwner;

        public BusinessOwnerController(VroomDbContext _context, BusinessOwnerRepository _businessOwner)
        {
            context = _context;
            businessOwner = _businessOwner;
        }

        [Route("Index")]
        public IActionResult Index(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {
            //var Riders = businessOwner.Search(status: status, Name: Name, PhoneNumber: PhoneNumber, pageNumber: pageNumber, pageSize: pageSize);


            //ViewData["Riders"] = Riders;

            return View("index");
        }

    }
}
