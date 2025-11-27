// SharedModelResource.cs
// Doel: wrapper voor de resource manager zodat data‑annotation attributen en modellen
//      gelokaliseerde validatie- en modelteksten kunnen opvragen met voorspelbare keys.
// Gebruik: gebruikt door modellen en attributen om foutmeldingen en validatieteksten
//         uit de resx-bestanden te halen.
// Doelstellingen:
// - Centrale plek met consistente resource keys voor modelvalidatie (Required, StringLength, enz.).
// - Mogelijkheid om een specifieke cultuur te forceren via de `Culture` property voor testing of debugging.
// - Altijd een leesbare fallbacktekst bieden wanneer een resource ontbreekt.

using System.Resources;
using System.Globalization;

namespace Biblio_Models.Resources
{
    // Publieke resource-wrapper zodat DataAnnotation-attributen resourceteksten op naam kunnen ophalen
    public class SharedModelResource
    {
        private static ResourceManager? resourceMan;
        private static CultureInfo? resourceCulture;

        public SharedModelResource() { }

        /// <summary>
        /// De ResourceManager die de resource-bestanden laadt (SharedModelResource.resx).
        /// </summary>
        public static ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                {
                    resourceMan = new ResourceManager("Biblio_Models.Resources.SharedModelResource", typeof(SharedModelResource).Assembly);
                }
                return resourceMan;
            }
        }

        /// <summary>
        /// Overschrijft de cultuur die gebruikt wordt bij resource lookup (optioneel).
        /// </summary>
        public static CultureInfo? Culture
        {
            get => resourceCulture;
            set => resourceCulture = value;
        }

        // Exposeer de verwachte resource keys als statische properties zodat DataAnnotation
        // attributen ze kunnen gebruiken. Bied een fallback tekst wanneer de resource ontbreekt.
        public static string Required => ResourceManager.GetString("Required", resourceCulture) ?? "{0} is verplicht.";
        public static string StringLength => ResourceManager.GetString("StringLength", resourceCulture) ?? "{0} moet tussen {2} en {1} tekens bevatten.";
        public static string IsbnInvalid => ResourceManager.GetString("IsbnInvalid", resourceCulture) ?? "Ongeldig ISBN.";
    }
}