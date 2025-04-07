
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ViewModels;
using VROOM.Repositories;

namespace Delivery_System.Controllers
{
    public class RoleController : Controller
    {
        //private RoleRepository RoleManager;
        //public RoleController(RoleRepository roleManager)
        //{
        //    RoleManager = roleManager;
        //}

        //[HttpGet]
        //public IActionResult Add()
        //{
        //    var list = RoleManager.GetList().Select(r => new RoleViewModel
        //    {
        //        Id = r.Id,
        //        Name = r.Name,
        //    }).ToList();

        //    //ViewBag.Invalid = 0;
        //    return View(list);
        //}
        //[HttpPost]
        //public async Task<IActionResult> Add(string roleName)
        //{
        //    if (roleName.IsNullOrEmpty())
        //    {
        //        ViewBag.Invalid = 1;
        //        var list = RoleManager.GetList().Select(r => new RoleViewModel
        //        {
        //            Id = r.Id,
        //            Name = r.Name,
        //        }).ToList();
        //        return View(list);
        //    }
        //    else
        //    {
        //        var res = await RoleManager.Add(roleName);
        //        if (res.Succeeded)
        //        {
        //            ViewBag.Invalid = 2;
        //        }
        //        else
        //        {
        //            ViewBag.Invalid = 1;
        //        }
        //        var list = RoleManager.GetList().Select(r => new RoleViewModel
        //        {
        //            Id = r.Id,
        //            Name = r.Name,
        //        }).ToList();
        //        return View(list);
        //    }

        //}
    }
}
