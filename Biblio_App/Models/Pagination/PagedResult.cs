using System;
using System.Collections.Generic;

namespace Biblio_App.Models.Pagination
{
    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }

        // Match API contract and avoid null collections
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    }
}