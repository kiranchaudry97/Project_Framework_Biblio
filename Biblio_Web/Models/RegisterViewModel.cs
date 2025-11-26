using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "FullName")]
        public string? FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "ConfirmPassword")]
        public string? ConfirmPassword { get; set; }
    }
}
