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
    public record GetDocumentJobQuery(string IndexName, string Id) : IRequest<DocumentJobRequest>;

    public class GetDocumentJobQueryHandler : IRequestHandler<GetDocumentJobQuery, DocumentJobRequest?>
    {
        private readonly IDatabaseService _databaseService;

        public GetDocumentJobQueryHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<DocumentJobRequest?> Handle(GetDocumentJobQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _databaseService.GetDocumentJob(request.IndexName, request.Id, cancellationToken);
            }
            catch(FileNotFoundException)
            {
                return null;
            }
        }
    }
}
