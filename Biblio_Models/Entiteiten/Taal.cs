using System;
using System.ComponentModel.DataAnnotations;

namespace Biblio_Models.Entiteiten
{
    // Entity: language / taal
    public class Taal : BaseEntiteit
    {
        [Required, StringLength(5)]
        public string Code { get; set; } = string.Empty; // e.g. "nl", "en"

        [Required, StringLength(120)]
        public string Naam { get; set; } = string.Empty; // display name

        public bool IsSystemTaal { get; set; } = false;

        // whether the language is active/available in the application
        public bool IsActive { get; set; } = true;

        // when the language record was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString() => $"{Code} - {Naam}";
    }
}