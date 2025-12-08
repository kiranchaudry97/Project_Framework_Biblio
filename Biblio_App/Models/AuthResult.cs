using System;

namespace Biblio_App.Models
{
    public class AuthResult
    {
        public string? AccessToken { get; set; }
        public DateTime? Expires { get; set; }
        public bool Success => !string.IsNullOrEmpty(AccessToken);
    }
}
