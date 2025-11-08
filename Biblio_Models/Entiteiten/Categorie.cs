// Patronen aanwezig in dit bestand:
// - Entiteit voor categorieën met validatie
// - Gebruikt in LINQ queries en soft-delete filtering

// Doel: Categorie-entiteit met naam en relatie naar boeken.
// Beschrijving: Bevat naam met lengtevalidatie en navigatiecollectie naar gekoppelde boeken.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblio_Models.Entiteiten
{
    public class Categorie : BaseEntiteit
    {
        [Required, StringLength(120)]
        public string Naam { get; set; } = string.Empty;
        public ICollection<Boek> Boeken { get; set; } = new List<Boek>();

    }
}
