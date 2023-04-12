using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using KnowledgeMining.Domain.Entities;

namespace KnowledgeMining.UI.Helpers
{
    public class GetCurrentLanguage
    {
        public static string GetLanguageCode()
        {
            CultureInfo currentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(System.Threading.Thread.CurrentThread.CurrentUICulture.Name);
            return currentCulture.TwoLetterISOLanguageName;
        }

        public static string GetLocalizedText(Localization obj)
        {
            string currentLanguage = GetLanguageCode();

            if (obj == null) return string.Empty;

            if (string.IsNullOrWhiteSpace(obj.Fr)) return obj.En ?? string.Empty;

            if (currentLanguage.Equals("fr")) return obj.Fr;

            return obj.En ?? string.Empty;


        }
    }
}
