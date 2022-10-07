using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexConfigurationDisplay
    {
        [JsonPropertyName("footer")]
        public string[]? Footer { get; set; }
    }
}
