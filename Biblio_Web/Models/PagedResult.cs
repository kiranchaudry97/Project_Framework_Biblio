using System;
using System.Collections.Generic;

namespace Biblio_Web.Models
{
    // Generiek model voor gepagineerde resultaten
    // Wordt vaak gebruikt in API responses en lijsten
    public class PagedResult<T>
    {
        // Items van de huidige pagina
        // Nooit null dankzij Array.Empty<T>()
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();

        // Totaal aantal items (zonder paginatie)
        public int Total { get; set; }

        // Huidige pagina (meestal 1-based)
        public int Page { get; set; }

        // Aantal items per pagina
        public int PageSize { get; set; }
    }
}
