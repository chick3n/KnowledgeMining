using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Infrastructure.Services.Database.Models
{
    public class JobDocument : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = default(DateTimeOffset?);
        public ETag ETag { get; set; } = default(ETag);
        public string? Title { get; set; }
    }
}
