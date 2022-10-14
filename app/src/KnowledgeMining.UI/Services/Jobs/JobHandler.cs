using MediatR;

namespace KnowledgeMining.UI.Services.Jobs
{
    public class JobHandler
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public JobHandler(IMediator mediator,
            ILogger<JobHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


    }
}
