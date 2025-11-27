// 1) //LINQ - Boek gebruikt in queries (Where, FirstOrDefault, AnyAsync)
// 2) //lambda expression - used in LINQ predicates when querying boeken
// 3) //CRUD - Boek participates in create/read/update/delete via DbContext

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
    public class Boek : BaseEntiteit
    {
        [Required(ErrorMessage = "Titel is verplicht."), StringLength(200, ErrorMessage = "{0} moet tussen {2} en {1} tekens bevatten.")] 
        public string Titel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Auteur is verplicht."), StringLength(200, ErrorMessage = "{0} moet tussen {2} en {1} tekens bevatten.")] 
        public string Auteur { get; set; } = string.Empty;

        [StringLength(17, ErrorMessage = "{0} moet maximaal {1} tekens bevatten.")]
        [RegularExpression(@"^(?:97[89])?\d{9}(\d|X)$", ErrorMessage = "Ongeldig ISBN.")]
        public string Isbn { get; set; } = string.Empty;


        // FK → Category
        [Required(ErrorMessage = "Categorie is verplicht.")]
        public int CategorieID { get; set; }
        public Categorie? categorie { get; set; }


        public ICollection<Lenen> leent { get; set; } = new List<Lenen>();

        [NotMapped]
        public string DisplayName => string.IsNullOrWhiteSpace(Auteur) ? Titel : $"{Titel} — {Auteur}";
    }
}




