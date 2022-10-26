using KnowledgeMining.Application.Documents.Queries.GetDocument;
using KnowledgeMining.Application.Documents.Queries.GetDocuments;
using KnowledgeMining.Domain.Enums;
using SearchDocument = KnowledgeMining.Application.Documents.Queries.GetDocuments.Document;
using UploadDocument = KnowledgeMining.Application.Documents.Commands.UploadDocument.Document;

namespace KnowledgeMining.Application.Common.Interfaces
{
    public interface IStorageService
    {
        Task<GetDocumentsResponse> GetDocuments(string container, string? searchPrefix, int pageSize, string? continuationToken, CancellationToken cancellationToken);
        Task<GetDocumentResponse> GetDocument(string key, string container, string filename, CancellationToken cancellationToken);
        Task SetDocumentTraits(SearchDocument document, DocumentTraits traits, CancellationToken cancellationToken);
        Task<IEnumerable<SearchDocument>> UploadDocuments(string containerName, IEnumerable<UploadDocument> documents, CancellationToken cancellationToken);
        ValueTask<byte[]> DownloadJobDocument(string documentName, CancellationToken cancellationToken);
        ValueTask<byte[]> DownloadDocument(string documentName, CancellationToken cancellationToken);
        ValueTask<byte[]> DownloadSource(string documentName, CancellationToken cancellationToken);
        ValueTask DeleteDocument(string documentName, CancellationToken cancellationToken);
    }
}