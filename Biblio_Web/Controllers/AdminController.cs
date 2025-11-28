using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Biblio_Web.Models;
using System.Collections.Generic;

namespace Biblio_Web.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users(string? search, string? role)
        {
            ViewBag.Search = search;
            ViewBag.SelectedRole = role;
            // exclude the 'Lid' role from the UI filters
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null && n != "Lid").ToList();

            var users = _userManager.Users.ToList();
            var model = new List<UserViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                // apply filters: search on username/email, role on roles
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim();
                    if (!((u.UserName ?? string.Empty).Contains(s) || (u.Email ?? string.Empty).Contains(s)))
                        continue;
                }
                if (!string.IsNullOrWhiteSpace(role))
                {
                    if (!roles.Contains(role)) continue;
                }

                model.Add(new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Blocked = (u as AppUser)?.IsBlocked ?? false,
                    Roles = roles.ToList()
                });
            }

            return View(model);
        }

        // GET: Admin/EditRoles/{userId}
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new AdminEditRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty
            };

            // exclude 'Lid' from editable roles
            var allRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null && n != "Lid").ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            model.Roles = allRoles.Select(r => new Biblio_Web.Models.RoleCheckbox { RoleName = r!, IsSelected = userRoles.Contains(r!) }).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRoles(AdminEditRolesViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selected = vm.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToArray();

            var toAdd = selected.Except(currentRoles).ToArray();
            var toRemove = currentRoles.Except(selected).ToArray();

            if (toAdd.Length > 0)
                await _userManager.AddToRolesAsync(user, toAdd);
            if (toRemove.Length > 0)
                await _userManager.RemoveFromRolesAsync(user, toRemove);

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCreateUserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new AppUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                EmailConfirmed = true
            };

            var res = await _userManager.CreateAsync(user, vm.Password);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            // assign roles (exclude 'Lid')
            if (vm.IsAdmin && await _roleManager.RoleExistsAsync("Admin"))
                await _userManager.AddToRoleAsync(user, "Admin");
            if (vm.IsStaff && await _roleManager.RoleExistsAsync("Medewerker"))
                await _userManager.AddToRoleAsync(user, "Medewerker");

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Delete/{userId}
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new AdminDeleteUserViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            };

            return View(model);
        }

        // POST: Admin/Delete/{userId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // delete the user
            await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/EditRolesIndex - show list of users with links to edit roles
        public async Task<IActionResult> EditRolesIndex()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                model.Add(new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Blocked = (u as AppUser)?.IsBlocked ?? false,
                    Roles = roles.ToList()
                });
            }

            return View("EditRolesIndex", model);
        }
    }
}
