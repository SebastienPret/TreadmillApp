using System.Globalization;
using System.Threading;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Services
{
    public static class LocalizationService
    {
        public static void SetLanguage(string languageCode) // ex: "fr", "en"
        {
            var culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Met à jour la ressource globale
            AppResources.Culture = culture;
        }
    }
}