using System.Collections.Generic;

namespace Biblio_Web.Models
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Blocked { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
