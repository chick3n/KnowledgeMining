namespace KnowledgeMining.UI.Pages.Services.Models
{
    public enum ServiceAction
    {
        Start,
        Refresh,
        Stop
    }

    public enum ServiceType
    {
        ExtractiveSummary,
        AbstractiveSummary
    }

    public enum ServiceState
    {
        Unknown,
        Pending,
        InProgress,
        Complete,
        Error
    }

    public record ServiceEvent(ServiceAction Action, ServiceType Service);
}
