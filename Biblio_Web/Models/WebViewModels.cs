using System.Collections.Generic;

namespace Biblio_Web.Models
{
    public class CreateUserViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsStaff { get; set; } = true;
    }

    public class RoleCheckbox
    {
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class AdminEditRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<RoleCheckbox> Roles { get; set; } = new List<RoleCheckbox>();
    }
}
