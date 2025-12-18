// 1) //LINQ - used when querying Leden collection (Where, AnyAsync)
// 2) //lambda expression - used in LINQ predicates
// 3) //CRUD - this entity participates in create/read/update/delete operations via DbContext

// - Entiteit voor leden met validatieattributen
// - Wordt gebruikt in queries (LINQ) en soft-delete filtering

// Doel: Lid-entiteit met validatie en relatie naar uitleningen.
// Beschrijving: Bevat voornaam, naam, e-mail (uniek in DB), telefoon;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biblio_Models.Resources;

namespace Biblio_Models.Entiteiten
{
    public class Lid : BaseEntiteit
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Voornaam { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string AchterNaam { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Telefoon { get; set; }

        public ICollection<Lenen> Leningens { get; set; } = new List<Lenen>();

        [NotMapped]
        public string FullName => (Voornaam + " " + AchterNaam).Trim();

    }

    [Table("LocalLeden")]
    public class LocalLid
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Voornaam { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string AchterNaam { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Telefoon { get; set; }

        [NotMapped]
        public string FullName => (Voornaam + " " + AchterNaam).Trim();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<LocalLenen>? LocalLeningens { get; set; }
    }
}
