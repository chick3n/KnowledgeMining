namespace KnowledgeMining.Application.Common.Options
{
    public class QueueOptions
    {
        public const string Queue = "Queue";

        public string? Endpoint { get; set; }
        public string? ExtractiveSummaryRequests { get; set; }
        public string? AbstractiveSummaryRequests { get; set; }
        public string? PromptRequests { get; set; }
        public string? ConnectionString { get; set; }

    }
}
