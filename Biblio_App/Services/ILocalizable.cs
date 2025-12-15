namespace Biblio_App.Services
{
    /// <summary>
    /// Simple contract for pages and viewmodels that can update their localized strings at runtime.
    /// AppShell will call this when the language is changed.
    /// </summary>
    public interface ILocalizable
    {
        void UpdateLocalizedStrings();
    }
}
