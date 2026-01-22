using System;
using System.Collections.Generic;

namespace Biblio_App.Models.Pagination
{
    // PagedResult<T>
    // Client-side model voor gepagineerde resultaten.
    // Wordt gebruikt binnen de MAUI app (ViewModels)
    // en is afgestemd op het API-contract.
    public class PagedResult<T>
    {
        // Huidige pagina (1-based index)
        public int Page { get; set; }

        // Aantal items per pagina
        public int PageSize { get; set; }

        // Totaal aantal items beschikbaar
        public int Total { get; set; }

        // Totaal aantal pagina's
        public int TotalPages { get; set; }

        // Items voor de huidige pagina
        // IEnumerable wordt gebruikt voor flexibiliteit
        // Array.Empty voorkomt null-referenties
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    }
}
