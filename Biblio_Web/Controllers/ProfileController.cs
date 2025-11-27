using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Biblio_Web.Models;

namespace Biblio_Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(UserManager<AppUser> userManager) => _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var vm = new ProfileViewModel { Id = user.Id, Email = user.Email ?? string.Empty, FullName = user.FullName ?? string.Empty };
            return View(vm);
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var vm = new ProfileViewModel { Id = user.Id, Email = user.Email ?? string.Empty, FullName = user.FullName ?? string.Empty };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();
            user.FullName = vm.FullName;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }
            TempData["Message"] = "Profiel opgeslagen.";
            return RedirectToAction(nameof(Index));
        }

        // Delete (self-delete) - optional
        public async Task<IActionResult> Delete()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(new ProfileViewModel { Id = user.Id, Email = user.Email ?? string.Empty, FullName = user.FullName ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var res = await _userManager.DeleteAsync(user);
            if (!res.Succeeded)
            {
                TempData["Message"] = "Kon gebruiker niet verwijderen.";
                return RedirectToAction(nameof(Index));
            }
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
