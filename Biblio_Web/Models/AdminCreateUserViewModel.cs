using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    public class AdminCreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }
        public bool IsStaff { get; set; }
    }
}