using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    public class ChangePasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // For normal users
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string? ConfirmPassword { get; set; }

        // internal flag to indicate admin resetting another user's password
        public bool IsAdminChange { get; set; }
    }
}