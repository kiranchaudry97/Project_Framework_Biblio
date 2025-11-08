// Patronen aanwezig in dit bestand:
// - Entiteit voor uitlening met relaties naar Book en Member
// - Velden voor datums (StartDate, DueDate, ReturnedAt) en soft-delete via BaseEntity

// Doel: Uitlening-entiteit met relaties naar Boek en Lid, data voor periodes en status.
// Beschrijving: Bewaart start-, eind- en inleverdatum en koppelt boek en lid; heeft IsClosed afgeleid van ReturnedAt.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblio_Models.Entiteiten
{
    public class Lenen : BaseEntiteit
    {
        public int BoekId { get; set; }
        public Boek? Boek { get; set; }
        public int LidId { get; set; }
        public Lid? Lid { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }

        // Hulpstatus (optioneel) — gebruik IsDeleted/ReturnedAt voor logica

        public bool IsClosed { get; set; }


    }
}
