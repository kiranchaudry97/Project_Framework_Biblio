// 1) //LINQ - Lenen wordt gebruikt in queries to filter loans by user/book/date
// 2) //lambda expression - predicates over loans (Where(...=>...))
// 3) //CRUD - Lenen participates in create/read/update/delete operations via DbContext
// - Entiteit voor uitlening met relaties naar Book en Member
// - Velden voor datums (StartDate, DueDate, ReturnedAt) en soft-delete via BaseEntity

// Doel: Uitlening-entiteit met relaties naar Boek en Lid, data voor periodes en status.
// Beschrijving: Bewaart start-, eind- en inleverdatum en koppelt boek en lid; heeft IsClosed afgeleid van ReturnedAt.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biblio_Models.Entiteiten
{
    public class Lenen : BaseEntiteit
    {
        public int BoekId { get; set; }
        [ForeignKey("BoekId")]
        public Boek? Boek { get; set; }
        public int LidId { get; set; }
        [ForeignKey("LidId")]
        public Lid? Lid { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }

        // Hulpstatus (optioneel) — gebruik IsDeleted/ReturnedAt voor logica

        public bool IsClosed { get; set; }


    }

    // Local variant voor gebruik op device: eigen primary key type en verwijzing naar LocalLid
    [Table("LocalLeningens")]
    public class LocalLenen
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public int BoekId { get; set; }
        [ForeignKey("BoekId")]
        public Boek? Boek { get; set; }

        public int LidId { get; set; }
        [ForeignKey("LidId")]
        public Lid? Lid { get; set; }

        // Optional reference to local member when working fully offline
        public long? LocalLidId { get; set; }
        [ForeignKey("LocalLidId")]
        public LocalLid? LocalLid { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public bool IsClosed { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
