using System.Collections.Generic;

namespace Biblio_App.Models
{
    public class ApiPagedResult<T>
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public int total { get; set; }
        public int totalPages { get; set; }
        public List<T> items { get; set; } = new();
    }
}
