using System;

namespace Biblio_Web.ApiModels
{
    public class UitleningDto
    {
        public int Id { get; set; }
        public int BoekId { get; set; }
        public string? BoekTitel { get; set; }
        public int LidId { get; set; }
        public string? LidNaam { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
    }
}