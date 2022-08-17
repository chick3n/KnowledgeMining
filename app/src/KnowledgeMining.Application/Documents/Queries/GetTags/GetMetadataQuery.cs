using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Documents.Queries.GetTags
{
    public record GetMetadataQuery() : IRequest<DocumentTag[]>;

    public class GetMetadataQueryHandler : IRequestHandler<GetMetadataQuery, DocumentTag[]>
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public GetMetadataQueryHandler(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public Task<DocumentTag[]> Handle(GetMetadataQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_storageOptions.Value.Metadata);
        }
    }
}
