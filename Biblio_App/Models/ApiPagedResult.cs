using System.Collections.Generic;

namespace Biblio_App.Models
{
    // ApiPagedResult<T>
    // Generieke DTO die een gepagineerd API-resultaat voorstelt.
    // Wordt gebruikt door de MAUI app om lijsten op te halen
    // met paging-informatie (boeken, leden, leningen, ...).
    public class ApiPagedResult<T>
    {
        // Huidige pagina (1-based index)
        // Komt overeen met de 'page' parameter in de API-call
        public int page { get; set; }

        // Aantal items per pagina
        public int pageSize { get; set; }

        // Totaal aantal items in de database
        // Ongeacht paging
        public int total { get; set; }

        // Totaal aantal beschikbare pagina's
        public int totalPages { get; set; }

        // De effectieve items voor deze pagina
        // Generiek zodat dit herbruikbaar is
        // (bv. BoekDto, LidDto, LenenDto, ...)
        public List<T> items { get; set; } = new();
    }
}
