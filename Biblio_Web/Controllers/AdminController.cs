using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten; // for AppUser
using Biblio_Web.Models; // view models
using Microsoft.EntityFrameworkCore;

namespace Biblio_Web.Controllers
{
    // Toegankelijk voor Admin en Medewerker
    [Authorize(Roles = "Admin,Medewerker")]
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
        public async Task<IActionResult> Users(string search, string role, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            // Provide available roles to the view for the role filter
            var allRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null).ToList();

            // Exclude internal/member-only role 'Lid' from the admin UI role filter
            allRoles = allRoles.Where(r => !string.Equals(r, "Lid", System.StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.Roles = allRoles;
            ViewBag.SelectedRole = role ?? string.Empty;

            // start from IQueryable to allow efficient paging
            System.Collections.Generic.IEnumerable<AppUser> usersEnumerable;

            if (!string.IsNullOrWhiteSpace(role))
            {
                // Get users in the selected role
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                usersEnumerable = usersInRole;
            }
            else
            {
                usersEnumerable = _userManager.Users.ToList();
            }

            // Apply search if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                usersEnumerable = usersEnumerable.Where(u => ((u.UserName ?? "").Contains(s) || (u.Email ?? "").Contains(s)));
            }

            var total = usersEnumerable.Count();

            var usersPaged = usersEnumerable.OrderBy(u => u.UserName).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var modelItems = new System.Collections.Generic.List<UserViewModel>();
            foreach (var u in usersPaged)
            {
                var rolesForUser = await _userManager.GetRolesAsync(u);
                modelItems.Add(new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Roles = rolesForUser.ToList(),
                    Blocked = (u as AppUser)?.IsBlocked ?? false
                });
            }

            var result = new PagedResult<UserViewModel>
            {
                Items = modelItems,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            ViewData["Search"] = search ?? string.Empty;
            // Return explicit view path to point to Views/Users after renaming folder
            return View("~/Views/Users/Users.cshtml", result);
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

            // If current user is not Admin, remove Admin role from the list so they cannot assign it
            if (!User.IsInRole("Admin"))
            {
                allRoles = allRoles.Where(r => !string.Equals(r, "Admin", System.StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            model.Roles = allRoles.Select(r => new Biblio_Web.Models.RoleCheckbox { RoleName = r!, IsSelected = userRoles.Contains(r!) }).ToList();

            // Use view in Views/Users after rename
            return View("~/Views/Users/EditRoles.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRoles(AdminEditRolesViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selected = vm.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToArray();

            // Enforce that only Admin users can add/remove the Admin role
            if (!User.IsInRole("Admin"))
            {
                // Remove Admin from selected if present
                selected = selected.Where(r => !string.Equals(r, "Admin", System.StringComparison.OrdinalIgnoreCase)).ToArray();
                // Also ensure Admin cannot be removed by non-admin: if target user currently has Admin, keep it
                if (currentRoles.Contains("Admin"))
                {
                    // make sure Admin remains in selected
                    selected = selected.Concat(new[] { "Admin" }).Distinct().ToArray();
                }
            }

            var toAdd = selected.Except(currentRoles).ToArray();
            var toRemove = currentRoles.Except(selected).ToArray();

            // If current user is not Admin, ensure Admin role is not removed or added
            if (!User.IsInRole("Admin"))
            {
                toAdd = toAdd.Where(r => !string.Equals(r, "Admin", System.StringComparison.OrdinalIgnoreCase)).ToArray();
                toRemove = toRemove.Where(r => !string.Equals(r, "Admin", System.StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            if (toAdd.Length > 0)
                await _userManager.AddToRolesAsync(user, toAdd);
            if (toRemove.Length > 0)
                await _userManager.RemoveFromRolesAsync(user, toRemove);

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/UserRoles/{id}
        [HttpGet]
        public async Task<IActionResult> UserRoles(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null).ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new AdminEditRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = allRoles.Select(r => new Biblio_Web.Models.RoleCheckbox { RoleName = r!, IsSelected = userRoles.Contains(r!) }).ToList()
            };

            return View("~/Views/Users/Details.cshtml", model);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent deleting yourself
            var currentUserId = _userManager.GetUserId(User);
            if (string.Equals(currentUserId, user.Id, System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Je kunt jezelf niet verwijderen.";
                return RedirectToAction(nameof(Users));
            }

            var res = await _userManager.DeleteAsync(user);
            if (!res.Succeeded)
            {
                TempData["Error"] = string.Join("; ", res.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Message"] = "Gebruiker verwijderd.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Delete/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                Blocked = (user as AppUser)?.IsBlocked ?? false
            };

            return View("~/Views/Users/Delete.cshtml", vm);
        }

        // GET: Admin/CreateUser
        [Authorize(Roles = "Admin,Medewerker")]
        public IActionResult CreateUser()
        {
            return View("~/Views/Users/Create.cshtml", new CreateUserViewModel());
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Medewerker")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel vm)
        {
            if (!ModelState.IsValid) return View("~/Views/Users/Create.cshtml", vm);

            if (await _userManager.FindByEmailAsync(vm.Email) != null)
            {
                ModelState.AddModelError(string.Empty, "E-mail is al in gebruik.");
                return View("~/Views/Users/Create.cshtml", vm);
            }

            var user = new AppUser { UserName = vm.Email, Email = vm.Email, FullName = vm.FullName };
            var res = await _userManager.CreateAsync(user, vm.Password);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View("~/Views/Users/Create.cshtml", vm);
            }

            // Assign roles
            if (vm.IsStaff)
                await _userManager.AddToRoleAsync(user, "Medewerker");

            // Only Admins can assign Admin role
            if (vm.IsAdmin && User.IsInRole("Admin"))
                await _userManager.AddToRoleAsync(user, "Admin");

            TempData["Message"] = "Gebruiker aangemaakt.";
            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Roles
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles
                .Select(r => r.Name)
                .Where(n => n != null && !string.Equals(n, "Lid", System.StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            return View("~/Views/Admin/Roles.cshtml", roles);
        }

    }
}
