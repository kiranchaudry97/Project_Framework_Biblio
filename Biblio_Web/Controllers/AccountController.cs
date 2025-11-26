using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;

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
            return View(new LoginViewModel { ReturnUrl = returnUrl ?? string.Empty });
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
        public IActionResult Register() => View(new RegisterViewModel());

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
    }
}
