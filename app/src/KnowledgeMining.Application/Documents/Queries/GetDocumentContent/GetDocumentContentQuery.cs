using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Domain.Entities;
using MediatR;

namespace KnowledgeMining.Application.Documents.Queries.GetIndex
{
    public readonly record struct GetIndexResponse(IndexItem IndexItem);

    public readonly record struct GetIndexQuery(string IndexName) : IRequest<GetIndexResponse>;

    public class GetIndexQueryValidator : AbstractValidator<GetIndexQuery>
    {
        public GetIndexQueryValidator()
        {
            RuleFor(q => q.IndexName)
                .NotEmpty()
                .WithMessage("Index name must not be empty.");
        }
    }

    public class GetIndexQueryHandler : IRequestHandler<GetIndexQuery, GetIndexResponse>
    {
        private readonly IDatabaseService _databaseService;

        public GetIndexQueryHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<GetIndexResponse> Handle(GetIndexQuery request, CancellationToken cancellationToken)
        {
            var indexItem = await _databaseService.GetIndex(request.IndexName, cancellationToken);
            if (indexItem != null)
            {
                return new GetIndexResponse(indexItem);
            }

            throw new FileNotFoundException(request.IndexName);
        }
    }
}
