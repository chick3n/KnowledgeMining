using KnowledgeMining.Application.Common.Interfaces;
using KnowledgeMining.Domain.Entities.Jobs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Documents.Queries.Jobs
{
    public record GetDocumentJobsQuery(string IndexName) : IRequest<IList<DocumentJobRequest>>;

    public class GetDocumentJobsQueryHandler : IRequestHandler<GetDocumentJobsQuery, IList<DocumentJobRequest>>
    {
        private readonly IDatabaseService _databaseService;

        public GetDocumentJobsQueryHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IList<DocumentJobRequest>> Handle(GetDocumentJobsQuery request, CancellationToken cancellationToken)
        {
            return await _databaseService.GetDocumentJobs(request.IndexName, cancellationToken);
        }
    }
}
