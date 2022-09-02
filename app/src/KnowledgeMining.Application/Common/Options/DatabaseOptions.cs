using KnowledgeMining.Domain.Entities;

namespace KnowledgeMining.Application.Common.Options
{
    public class DatabaseOptions
    {
        public const string Database = "Database";

        public string? ConnectionString { get; set; }
        public string DatabaseName { get; set; } = "doccracker";
        public string ContainerName { get; set; } = "app";
        public string PartitionKey { get; set; } = "/id";


    }
}
