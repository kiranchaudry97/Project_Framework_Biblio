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
        [Required(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "Required"), StringLength(200, ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "StringLength")] 
        public string Titel { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "Required"), StringLength(200, ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "StringLength")] 
        public string Auteur { get; set; } = string.Empty;

        [StringLength(17, ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "StringLength")]
        [RegularExpression(@"^(?:97[89])?\d{9}(\d|X)$", ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "IsbnInvalid")]
        public string Isbn { get; set; } = string.Empty;


        // FK → Category
        [Required(ErrorMessageResourceType = typeof(SharedModelResource), ErrorMessageResourceName = "Required")]
        public int CategorieID { get; set; }
        public Categorie? categorie { get; set; }


        public ICollection<Lenen> leent { get; set; } = new List<Lenen>();

        [NotMapped]
        public string DisplayName => string.IsNullOrWhiteSpace(Auteur) ? Titel : $"{Titel} — {Auteur}";
    }
}




