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

    public record ServiceEvent(ServiceAction Action, ServiceType Service);
}
