using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Biblio_Web.Models;

namespace Biblio_Web.Controllers
{
    // Controller voor het beheren van het eigen gebruikersprofiel
    // Alleen toegankelijk voor ingelogde gebruikers
    [Authorize]
    public class ProfileController : Controller
    {
        // Identity UserManager voor gebruikersbeheer
        private readonly UserManager<AppUser> _userManager;

        // Dependency Injection van UserManager
        public ProfileController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // =====================================================
        // INDEX
        // =====================================================
        // Toont het profiel van de ingelogde gebruiker
        public async Task<IActionResult> Index()
        {
            // Haal de momenteel ingelogde gebruiker op
            var user = await _userManager.GetUserAsync(User);

            // Indien niet ingelogd of user niet gevonden → login vereisen
            if (user == null)
                return Challenge();

            // Maak ViewModel voor de view
            var vm = new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            };

            return View(vm);
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================
        // Toont het formulier om het eigen profiel te bewerken
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var vm = new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            };

            return View(vm);
        }

        // =====================================================
        // EDIT (POST)
        // =====================================================
        // Verwerkt wijzigingen aan het profiel
        [HttpPost]
        [ValidateAntiForgeryToken] // Bescherming tegen CSRF
        public async Task<IActionResult> Edit(ProfileViewModel vm)
        {
            // Valideer input
            if (!ModelState.IsValid)
                return View(vm);

            // Zoek gebruiker op basis van ID
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null)
                return NotFound();

            // Pas enkel toegestane velden aan
            user.FullName = vm.FullName;

            // Update gebruiker via Identity
            var res = await _userManager.UpdateAsync(user);

            // Indien fouten bij update
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                {
                    ModelState.AddModelError(string.Empty, e.Description);
                }
                return View(vm);
            }

            // Succesboodschap via TempData
            TempData["Message"] = "Profiel opgeslagen.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE (GET)
        // =====================================================
        // Toont bevestigingspagina om het eigen account te verwijderen
        // (optionele functionaliteit)
        public async Task<IActionResult> Delete()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            return View(new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            });
        }

        // =====================================================
        // DELETE (POST)
        // =====================================================
        // Verwijdert het eigen account definitief
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Basisvalidatie
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            // Zoek gebruiker
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Verwijder gebruiker via Identity
            var res = await _userManager.DeleteAsync(user);

            // Indien verwijderen mislukt
            if (!res.Succeeded)
            {
                TempData["Message"] = "Kon gebruiker niet verwijderen.";
                return RedirectToAction(nameof(Index));
            }

            // Uitloggen na accountverwijdering
            await HttpContext.SignOutAsync();

            // Terug naar startpagina
            return RedirectToAction("Index", "Home");
        }
    }
}
