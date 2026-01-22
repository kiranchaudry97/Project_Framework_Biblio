namespace Biblio_Web.Models
{
    // ViewModel gebruikt om een gebruiker te verwijderen (confirmatiescherm)
    // Bevat enkel leesbare informatie, geen validatie of logica
    public class AdminDeleteUserViewModel
    {
        // Unieke ID van de gebruiker (Identity)
        // Nodig om de juiste gebruiker te verwijderen
        public string UserId { get; set; } = string.Empty;

        // E-mailadres van de gebruiker
        // Wordt getoond ter bevestiging
        public string Email { get; set; } = string.Empty;

        // Volledige naam van de gebruiker
        // Helpt om fouten bij verwijderen te voorkomen
        public string FullName { get; set; } = string.Empty;
    }
}
