using System.Collections.Generic;

namespace Biblio_Web.Models
{
    public class AdminEditRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<RoleCheckbox> Roles { get; set; } = new();
    }

    public class RoleCheckbox
    {
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
