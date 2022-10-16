namespace KnowledgeMining.Application.Common.Options
{
    public class QueueOptions
    {
        public const string Queue = "Queue";

        public string? Endpoint { get; set; }
        public string? DocumentRequests { get; set; }
        public string? ConnectionString { get; set; }

    }
}
