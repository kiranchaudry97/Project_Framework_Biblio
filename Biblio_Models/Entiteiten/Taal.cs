using System.ComponentModel.DataAnnotations;

namespace Biblio_Models.Entiteiten
{
    public class Taal
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty; // e.g. "nl", "en"

        [Required]
        [MaxLength(120)]
        public string Naam { get; set; } = string.Empty; // e.g. "Nederlands", "English"

        public bool IsDefault { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
    }
}
