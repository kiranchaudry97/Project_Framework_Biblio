using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using Biblio_Web.Models;

namespace Biblio_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // Redirect to Identity area login page to avoid duplicate login views
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            var target = Url.Content("~/Identity/Account/Login");
            if (!string.IsNullOrEmpty(returnUrl))
                target += "?returnUrl=" + System.Net.WebUtility.UrlEncode(returnUrl);
            return Redirect(target);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Login(LoginViewModel vm)
        {
            var returnUrl = vm?.ReturnUrl;
            var target = Url.Content("~/Identity/Account/Login");
            if (!string.IsNullOrEmpty(returnUrl))
                target += "?returnUrl=" + System.Net.WebUtility.UrlEncode(returnUrl);
            return Redirect(target);
        }

        // GET fallback logout: available anonymously and redirects to Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
            }
            TempData["Message"] = "Je bent uitgelogd.";
            return Redirect(Url.Content("~/Identity/Account/Login"));
        }

        // POST logout (recommended)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> LogoutPost()
        {
            await _signInManager.SignOutAsync();
            TempData["Message"] = "Je bent uitgelogd.";
            return Redirect(Url.Content("~/Identity/Account/Login"));
        }

        // Redirect register to Identity UI
        [AllowAnonymous]
        public IActionResult Register() => Redirect(Url.Content("~/Identity/Account/Register"));

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Register(RegisterViewModel vm)
        {
            // Forward to Identity registration UI
            return Redirect(Url.Content("~/Identity/Account/Register"));
        }

        // --- Admin user management actions (use shared view models from Biblio_Models) ---

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UsersList()
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

            return View("Users", model);
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user is AppUser appUser)
            {
                appUser.IsBlocked = !appUser.IsBlocked;
                await _userManager.UpdateAsync(appUser);
            }

            return RedirectToAction(nameof(UsersList));
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UserRoles(string id)
        {
            // Redirect legacy user-roles view to the unified ChangePassword page (admin can reset there)
            if (string.IsNullOrEmpty(id)) return BadRequest();
            return RedirectToAction(nameof(ChangePassword), new { id });
        }

        [Authorize]
        public async Task<IActionResult> ChangePassword(string? id)
        {
            // id is optional: if provided and caller is admin, admin can change other user's password
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            ChangePasswordViewModel vm = new ChangePasswordViewModel();

            if (!string.IsNullOrEmpty(id) && User.IsInRole("Admin"))
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();
                vm.UserId = user.Id;
                vm.UserName = user.UserName ?? string.Empty;
                vm.IsAdminChange = true;
            }
            else
            {
                vm.UserId = currentUser.Id;
                vm.UserName = currentUser.UserName ?? string.Empty;
                vm.IsAdminChange = false;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // If admin changing other user's password
            if (vm.IsAdminChange && User.IsInRole("Admin") && vm.UserId != currentUser.Id)
            {
                var user = await _userManager.FindByIdAsync(vm.UserId);
                if (user == null) return NotFound();

                // generate token and reset
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var res = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
                if (res.Succeeded)
                {
                    TempData["Message"] = "Wachtwoord gewijzigd.";
                    return RedirectToAction("Users", "Admin");
                }
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            // Normal user change: require current password
            var targetUser = await _userManager.FindByIdAsync(vm.UserId);
            if (targetUser == null) return NotFound();

            var check = await _userManager.CheckPasswordAsync(targetUser, vm.CurrentPassword ?? string.Empty);
            if (!check)
            {
                ModelState.AddModelError(string.Empty, "Huidig wachtwoord is onjuist.");
                return View(vm);
            }

            var changeRes = await _userManager.ChangePasswordAsync(targetUser, vm.CurrentPassword ?? string.Empty, vm.NewPassword);
            if (!changeRes.Succeeded)
            {
                foreach (var e in changeRes.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            TempData["Message"] = "Wachtwoord succesvol gewijzigd.";
            return RedirectToAction("Index", "Profile");
        }
    }
}
