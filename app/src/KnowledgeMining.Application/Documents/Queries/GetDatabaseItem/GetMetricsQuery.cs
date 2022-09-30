using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Domain.Entities;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.GetDatabaseItem
{
    public readonly record struct GetMetrics(Metrics Metrics);

    public readonly record struct GetDatabaseItemMetricsQuery(string IndexName) : IRequest<GetMetrics>;

    public class GetDatabaseItemMetricsQueryValidator : AbstractValidator<GetDatabaseItemMetricsQuery>
    {
        public GetDatabaseItemMetricsQueryValidator()
        {
            RuleFor(q => q.IndexName)
                .NotEmpty()
                .WithMessage("Metrics index name must not be empty.");
        }
    }

    public class GetDatabaseItemMetricsQueryHandler : IRequestHandler<GetDatabaseItemMetricsQuery, GetMetrics>
    {
        private readonly IDatabaseService _databaseService;

        public GetDatabaseItemMetricsQueryHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<GetMetrics> Handle(GetDatabaseItemMetricsQuery request, CancellationToken cancellationToken)
        {
            var metrics = await _databaseService.GetMetrics(request.IndexName, cancellationToken);
            if (metrics != null)
            {
                return new GetMetrics(metrics);
            }

            throw new FileNotFoundException(request.IndexName);
        }
    }
}
