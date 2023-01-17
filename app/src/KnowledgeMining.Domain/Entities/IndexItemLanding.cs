using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItemLanding
    {
        [JsonPropertyName("page")]
        public string PagePath { get; set; }

        [JsonPropertyName("scripts")]
        public List<string> Scripts {get; set; }
    }
}
