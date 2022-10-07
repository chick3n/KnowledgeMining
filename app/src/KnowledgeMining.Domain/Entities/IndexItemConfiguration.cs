using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public class IndexItemConfiguration
    {
        [JsonPropertyName("timespan")]
        public IndexItemConfigurationTimeSpan? TimeSpan { get; set; }

        [JsonPropertyName("display")]
        public IndexConfigurationDisplay? Display { get; set; }
    }
}
