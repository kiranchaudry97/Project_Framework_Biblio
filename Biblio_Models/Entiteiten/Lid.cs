// 1) //LINQ - used when querying Leden collection (Where, AnyAsync)
// 2) //lambda expression - used in LINQ predicates
// 3) //CRUD - this entity participates in create/read/update/delete operations via DbContext

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
using Biblio_Models.Resources;

namespace Biblio_Models.Entiteiten
{
    public class Lid : BaseEntiteit
    {
        [Required(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "Required"), StringLength(100, ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "StringLength")]
        public string Voornaam { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "Required"), StringLength(100, ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "StringLength")]
        public string AchterNaam { get; set; } = string.Empty;

        [EmailAddress(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "EmailAddress")]
        public string? Email { get; set; }

        [Phone(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "Phone")]
        public string? Telefoon { get; set; }

        [StringLength(300, ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "StringLength")]
        public string? Adres { get; set; }

        public ICollection<Lenen> Leningens { get; set; } = new List<Lenen>();

    }
}
