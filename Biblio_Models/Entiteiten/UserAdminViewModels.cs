using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Biblio_Models.Entiteiten
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Gebruiker")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Blokkeren of deblokkeren")]
        public bool Blocked { get; set; }

        [Display(Name = "Rollen")]
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UserRolesViewModel
    {
        [Display(Name = "User")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Roles")]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
