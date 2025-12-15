using System;

namespace Biblio_Models.Entiteiten
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool Revoked { get; set; }
        public string? ReplacedByToken { get; set; }
    }
}
