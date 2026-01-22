namespace Biblio_Web.Models
{
    // ViewModel voor het tonen van gebruikersprofielinformatie
    // Wordt gebruikt op de profielpagina van de gebruiker
    public class ProfileViewModel
    {
        // Unieke ID van de gebruiker (Identity)
        // Wordt meestal intern gebruikt
        public string Id { get; set; } = string.Empty;

        // E-mailadres van de gebruiker
        public string Email { get; set; } = string.Empty;

        // Volledige naam van de gebruiker
        public string FullName { get; set; } = string.Empty;
    }
}
