// 1) //LINQ - BaseEntiteit-derived types are queried via LINQ
// 2) //lambda expression - used in LINQ predicates across entities
// 3) //CRUD - BaseEntiteit provides Id/IsDeleted used in CRUD operations
// Doel: Basisklasse voor entiteiten met soft-delete ondersteuning (IsDeleted, DeletedAt).
// Beschrijving: Biedt Id, IsDeleted en DeletedAt voor logische verwijderingen op alle afgeleide entiteiten.


using System; // using directives voor benodigde namespaces
using System.Collections.Generic; // generieke collecties voor lijsten
using System.ComponentModel.DataAnnotations; // data-annotaties voor validatie
using System.Linq; // LINQ functionaliteit voor queries
using System.Text; // tekstmanipulatie
using System.Threading.Tasks; // asynchrone taken

namespace Biblio_Models.Entiteiten // namespace voor entiteiten in Biblio_Models
{
    public class BaseEntiteit // Basisklasse voor entiteiten met soft-delete functionaliteit
    {
public int Id { get; set; } // Unieke identifier voor de entiteit
        public bool IsDeleted { get; set; }// Vlag voor soft-delete status (true als verwijderd)
        public DateTime? DeletedAt { get; set; } // Tijdstip van verwijdering (null als niet verwijderd)
    }
}