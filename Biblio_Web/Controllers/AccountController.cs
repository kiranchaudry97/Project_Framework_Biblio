using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using Biblio_Web.Models;
using System;

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

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirect to Identity-area Razor Page for login
            var target = $"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
            return Redirect(target);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email ?? string.Empty);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Onbekende gebruiker");
                return View(vm);
            }

            var res = await _signInManager.PasswordSignInAsync(user, vm.Password ?? string.Empty, vm.RememberMe, false);
            if (res.Succeeded) return Redirect(vm.ReturnUrl ?? "/");

            ModelState.AddModelError(string.Empty, "Ongeldig wachtwoord");
            return View(vm);
        }

        // GET fallback logout: available anonymously and redirects to Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            // Sign out if still signed in, then redirect to Login with message
            if (User?.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
            }
            TempData["Message"] = "Je bent uitgelogd.";
            return RedirectToAction("Login", "Account");
        }

        // POST logout (recommended)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> LogoutPost()
        {
            await _signInManager.SignOutAsync();
            TempData["Message"] = "Je bent uitgelogd.";
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult Register() 
        {
            // Redirect to Identity-area Razor Page for register
            return Redirect("/Identity/Account/Register");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var email = vm.Email ?? string.Empty;
            var password = vm.Password ?? string.Empty;
            var user = new AppUser { UserName = email, Email = email, FullName = vm.FullName };
            var res = await _userManager.CreateAsync(user, password);
            if (res.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Lid");
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
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

            return View("Users", model); // reuses Views/Admin/Users.cshtml (adjust view if needed)
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleBlock(string id, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user is AppUser appUser)
            {
                appUser.IsBlocked = !appUser.IsBlocked;
                await _userManager.UpdateAsync(appUser);
            }

            // Prefer explicit returnUrl if provided and local, otherwise use Referer header, otherwise go to Admin/Users
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction("Users", "Admin");
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UserRoles(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new UserRolesViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToList()
            };

            return View(vm); // create Views/Account/UserRoles.cshtml if needed
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new Biblio_Web.Models.ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                FullName = (user as AppUser)?.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };

            return View("~/Views/Profiel/Index.cshtml", vm);
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            // Redirect to Identity area change password page if available
            return RedirectToPage("/Account/Manage/ChangePassword", new { area = "Identity" });
        }

        [Authorize]
        public async Task<IActionResult> Details()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new Biblio_Web.Models.ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                FullName = (user as AppUser)?.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };

            return View("~/Views/Profiel/Details.cshtml", vm);
        }

        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new Biblio_Web.Models.ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                FullName = (user as AppUser)?.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };

            return View("~/Views/Profiel/Edit.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(Biblio_Web.Models.ProfileViewModel vm)
        {
            if (!ModelState.IsValid) return View("~/Views/Profiel/Edit.cshtml", vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only update FullName here. Email changes should be handled via Identity features.
            (user as AppUser)!.FullName = vm.FullName ?? string.Empty;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View("~/Views/Profiel/Edit.cshtml", vm);
            }

            TempData["Message"] = "Profiel bijgewerkt.";
            return RedirectToAction(nameof(Details));
        }
    }
}
