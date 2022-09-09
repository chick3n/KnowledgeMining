using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItemFieldNames
    {
        public const string WordCloud = "wordCloud";
        public const string AltTitle = "altTitle";
    }

    public class IndexItemFieldMapping
    {
        [JsonExtensionData]
        public Dictionary<string, object>? ExtensionData { get; set; }

        public string? GetMappedField(string? key)
        {
            if (ExtensionData == null)
                return null;

            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (ExtensionData.ContainsKey(key))
                return ExtensionData[key].ToString();

            return null;
        }
    }
}
