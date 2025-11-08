// 1) //LINQ - BaseEntiteit-derived types are queried via LINQ
// 2) //lambda expression - used in LINQ predicates across entities
// 3) //CRUD - BaseEntiteit provides Id/IsDeleted used in CRUD operations
// Doel: Basisklasse voor entiteiten met soft-delete ondersteuning (IsDeleted, DeletedAt).
// Beschrijving: Biedt Id, IsDeleted en DeletedAt voor logische verwijderingen op alle afgeleide entiteiten.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblio_Models.Entiteiten
{
    public class BaseEntiteit
    {
public int Id { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}