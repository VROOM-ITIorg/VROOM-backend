using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ViewModels;
using VROOM.Models;

namespace Delivery_System.Controllers
{
        [Authorize(Roles = "Admin")]
        [Route("vroom-admin/{controller}")]
    public class ManagerController : Controller
    {
            private readonly UserManager<User> _userManager;
            private readonly RoleManager<IdentityRole> _roleManager;

            public ManagerController(
                UserManager<User> userManager,
                RoleManager<IdentityRole> roleManager)
            {
                _userManager = userManager;
                _roleManager = roleManager;
            }

            public IActionResult AdminList()
            {
                // أكواد عرض قائمة المديرين
                return View();
            }

            [HttpGet]
            [Route("CreateAdmin")]
            public IActionResult CreateAdmin()
            {
                // تحضير قائمة الأدوار للاختيار منها
                ViewBag.Roles = _roleManager.Roles
                    .Where(r => r.Name != "User") // عدم إظهار دور المستخدم العادي
                    .Select(r => new SelectListItem { Value = r.Id })
                    .ToList();

                return View("Create");
            }

            [HttpPost]

            public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
            {
                if (ModelState.IsValid)
                {
                    // تجهيز بيانات المستخدم
                    var user = new User
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,


                    };

                    // معالجة الصورة الشخصية إذا وجدت
                    if (model.ProfilePicture != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await model.ProfilePicture.CopyToAsync(memoryStream);

                            // حفظ الصورة في ملف
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfilePicture.FileName);
                            string filePath = Path.Combine("wwwroot/images/profiles", fileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                memoryStream.Position = 0;
                                await memoryStream.CopyToAsync(fileStream);
                            }

                            // حفظ مسار الصورة في قاعدة البيانات
                        }
                    }

                    // إنشاء المستخدم
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        // إضافة المستخدم إلى الدور المختار
                        var role = await _roleManager.FindByIdAsync(model.RoleId);
                        if (role != null)
                        {
                            await _userManager.AddToRoleAsync(user, role.Name);
                        }

                        return RedirectToAction("AdminList", "Administration");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }

                // في حالة وجود خطأ، نقوم بإعادة تحضير قائمة الأدوار وإرجاع النموذج
                ViewBag.Roles = _roleManager.Roles
                    .Where(r => r.Name != "User")
                    .Select(r => new SelectListItem { Value = r.Id, Text = r.Name })
                    .ToList();

                return View(model);
            }
        }
    }

