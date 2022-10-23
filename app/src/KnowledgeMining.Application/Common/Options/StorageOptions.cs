using KnowledgeMining.Domain.Entities;

namespace KnowledgeMining.Application.Common.Options
{
    public class StorageOptions
    {
        public const string Storage = "Storage";

        public Uri? ServiceUri { get; set; }
        public string ContainerName { get; set; } = "documents";
        public string SourceContainerName { get; set; } = "source-docs";

        public DocumentTag[] Tags { get; set; } = Array.Empty<DocumentTag>();

        public DocumentTag[] Metadata { get; set; } = Array.Empty<DocumentTag>();

        public Dictionary<string, object> ConnectionStrings { get; set; } = new Dictionary<string, object>();
    }
}
