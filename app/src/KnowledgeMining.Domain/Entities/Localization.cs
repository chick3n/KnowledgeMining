using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;

namespace KnowledgeMining.Domain.Entities
{
    public class Localization
    {
        
        [JsonPropertyName("En")]
        public string?En{ get; set; }
        [JsonPropertyName("Fr")]
        public string?Fr{ get; set; }
    }
}
