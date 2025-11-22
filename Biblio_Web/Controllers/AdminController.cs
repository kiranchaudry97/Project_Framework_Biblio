using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Biblio_Web.ViewModels;

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
        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
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

            var allRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null).ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            model.Roles = allRoles.Select(r => new Biblio_Web.ViewModels.RoleCheckbox { RoleName = r!, IsSelected = userRoles.Contains(r!) }).ToList();

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
    }
}
