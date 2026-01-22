namespace Biblio_Web.Models
{
    // ViewModel gebruikt voor de foutpagina (Error view)
    // Geeft informatie over het huidige HTTP-request
    public class ErrorViewModel
    {
        // Unieke identifier van het request
        // Handig voor debugging en logging
        public string? RequestId { get; set; }

        // Bepaalt of de RequestId getoond moet worden in de view
        // True wanneer RequestId niet null of leeg is
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
