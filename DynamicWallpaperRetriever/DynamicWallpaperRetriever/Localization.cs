/**
 * For .Net supported culture codes, see: https://www.venea.net/web/culture_code
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    internal static class Localization
    {
        internal static HashSet<string> SupportedCultures = new HashSet<string>()
        {
            "",         // Invariant culture
            "en",       // Region-neutral English
            "en-au",
            "en-ca",
            "en-gb",
            "en-us",
            "fr",
            "fr-CA",
            "fr-FR",
            "fr-LU"
        };

        internal static void ApplyLocalization(string cultureName)
        {
            CultureInfo culture = CheckSupported(cultureName);
            if (culture != null)
            {
                ApplyLocalizationInternal(culture);
            }
            else
            {
                ApplyLocalizationInternal(CultureInfo.InvariantCulture);
            }
        }

        private static void ApplyLocalizationInternal(CultureInfo culture)
        {
            CultureInfo.CurrentUICulture = culture;
        }

        /// <summary>
        /// Checks whether a specific culture is supported as a localization on the current system.
        /// </summary>
        /// <param name="cultureName">The name of the culture to check.</param>
        /// <returns>Whether the specific culture's localization is supported by this application.</returns>
        internal static CultureInfo CheckSupported(string cultureName)
        {
            cultureName = cultureName.Trim();
            // Check app support
            bool supported = SupportedCultures.Contains(cultureName.ToLowerInvariant());
            if (!supported)
            {
                // Check region-neutral language support
                cultureName = cultureName.Substring(0, cultureName.IndexOf('-'));
                supported = SupportedCultures.Contains(cultureName.ToLowerInvariant());
                if (!supported) return null;
            }

            // Check system support
            try
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

        internal static CultureInfo GetSystemCulture()
        {
            return CultureInfo.InstalledUICulture;
        }

    }
}
