using FluentValidation;
using KnowledgeMining.Application.Common.Interfaces;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeMining.Application.Documents.Queries.GetDocumentContent
{
    public readonly record struct GetJobDocumentContentQuery(string RecordId) : IRequest<GetDocumentJobContentResponse?>;

    public class GetDocumentJobContentResponse
    {
        [JsonExtensionData]
        public Dictionary<string, object>? Response { get; set; }
    }

    public class GetJobDocumentsQueryValidator : AbstractValidator<GetJobDocumentContentQuery>
    {
        public GetJobDocumentsQueryValidator()
        {
            RuleFor(q => q.RecordId)
                .NotEmpty()
                .WithMessage("RecordId must not be empty.");
        }
    }

    public class GetJobDocumentsQueryHandler : IRequestHandler<GetJobDocumentContentQuery, GetDocumentJobContentResponse?>
    {
        private readonly IStorageService _storageService;

        public GetJobDocumentsQueryHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<GetDocumentJobContentResponse?> Handle(GetJobDocumentContentQuery request, CancellationToken cancellationToken)
        {
            var filename = $"{request.RecordId}.json";
            var bytes = await _storageService.DownloadJobDocument(filename, cancellationToken);
            if (bytes != null)
            {
                var data = System.Text.Encoding.UTF8.GetString(bytes);
                return JsonSerializer.Deserialize<GetDocumentJobContentResponse>(data);
            }

            throw new FileNotFoundException(filename);
        }
    }
}
