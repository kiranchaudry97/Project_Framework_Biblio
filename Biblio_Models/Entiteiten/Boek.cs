// 1) //LINQ - Boek gebruikt in queries (Where, FirstOrDefault, AnyAsync)
// 2) //lambda expression - used in LINQ predicates when querying boeken
// 3) //CRUD - Boek participates in create/read/update/delete via DbContext

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblio_Models.Entiteiten
{
    public class Boek : BaseEntiteit
    {
        [Required, StringLength(200)]
        public string Titel { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Auteur { get; set; } = string.Empty;

        [StringLength(17)]
        [RegularExpression(@"^(?:97[89])?\d{9}(\d|X)$", ErrorMessage = "Ongeldige ISBN.")]
        public string Isbn { get; set; } = string.Empty;


        // FK → Category
        [Required]
        public int CategorieID { get; set; }
        public Categorie? categorie { get; set; }


        public ICollection<Lenen> leent { get; set; } = new List<Lenen>();
    }
}


