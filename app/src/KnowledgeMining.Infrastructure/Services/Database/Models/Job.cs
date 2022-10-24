using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Infrastructure.Services.Database.Models
{
    internal class Job : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Index { get; set; }
        public string State { get; set; }
        public string CreatedBy { get; set; }
        public string Action { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
