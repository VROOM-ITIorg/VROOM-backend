using AdminArea.Managers;
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Models;
using ViewModels;
namespace AdminArea.Controllers
{
    public class BusinessOwnerController : Controller
    {
        private readonly BusinessOwnerManager _businessManager;
        public BusinessOwnerController(BusinessOwnerManager businessManager)
        {
            _businessManager = businessManager ?? throw new ArgumentNullException(nameof(businessManager));
        }




        public IActionResult Index()
        {
            var businessOwners = _businessManager.GetAllBusinessOwners();
            return View(businessOwners);
        }

      public IActionResult Details(int id)
        {
            var businessOwner = _businessManager.GetById(id);

            if (businessOwner == null)
                return NotFound();
            return View();
        }

        public IActionResult Add()
        {
            return View(new AddBusinessOwnerViewModel());
        }
        [HttpPost]
        public IActionResult Add(AddBusinessOwnerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var businessOwner = new BusinessOwner
            {
                BankAccount = model.BankAccount,
                BusinessType = model.BusinessType,
                UserID = model.UserID
            };

            _businessManager.AddBusinessOwner(businessOwner);

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var businessOwner = _businessManager.GetById(id);
            if (businessOwner == null) return NotFound();
            return View(businessOwner);
        }

        [HttpPost]
        public IActionResult Edit(int id, BusinessOwner model)
        {

          
            if (id != model.BusinessID)
                return BadRequest("ID Mismatch");

            if (!ModelState.IsValid)
                return View(model);

            var existingBusinessOwner = _businessManager.GetById(id);
            if (existingBusinessOwner == null)
                return NotFound();

 
            existingBusinessOwner.BankAccount = model.BankAccount;
            existingBusinessOwner.BusinessType = model.BusinessType;

           
            _businessManager.UpdateBusinessOwner(existingBusinessOwner);

   
            var updatedBusinessOwner = _businessManager.GetById(id);
            if (updatedBusinessOwner.BankAccount != model.BankAccount || updatedBusinessOwner.BusinessType != model.BusinessType)
            {
                return StatusCode(500, "Update failed.");
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public IActionResult Delete(int id)
        {
            var businessOwner = _businessManager.GetById(id);
            if (businessOwner == null) return NotFound();
            return View(businessOwner);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _businessManager.DeleteBusinessOwner(id);
            return RedirectToAction(nameof(Index));
        }

    }

}
