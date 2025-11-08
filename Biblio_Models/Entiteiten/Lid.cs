// Patronen aanwezig in dit bestand:
// - Entiteit voor leden met validatieattributen
// - Wordt gebruikt in queries (LINQ) en soft-delete filtering

// Doel: Lid-entiteit met validatie en relatie naar uitleningen.
// Beschrijving: Bevat voornaam, naam, e-mail (uniek in DB), telefoon en adres;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblio_Models.Entiteiten
{
    public class Lid : BaseEntiteit
    {
        [Required, StringLength(100)]
        public string Voornaam { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string AchterNaam { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;
       
        [Phone]
        public string? Telefoon { get; set; }

        [StringLength(300)]
        public string? Adres { get; set; }

        public ICollection<Lenen> Leningens { get; set; } = new List<Lenen>();

    }
}
