using System.Net.Mime;
using System.Text;

namespace KnowledgeMining.UI.Extensions
{
    public static class ResultsExtensions
    {
        public static IResult InlineFile(this IResultExtensions resultExtensions, byte[] fileContents, string fileName, string contentType, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);

            var fileType = fileName.GetFileExtension();

            switch(fileType)
            {
                case FileExtensions.EML:
                case FileExtensions.MSG:
                    return new InlineEmailResult(fileContents, fileName, contentType, fileType, cancellationToken);
            }

            return new InlineFileResult(fileContents, fileName, contentType, cancellationToken);
        }
    }



    class InlineEmailResult : IResult
    {
        private readonly byte[] _fileContents;
        private readonly string _fileName;
        private readonly string _contentType;
        private readonly string _fileType;
        private readonly CancellationToken _cancellationToken;

        public InlineEmailResult(byte[] fileContents, string fileName, string contentType, string fileType, CancellationToken cancellationToken)
        {
            _fileContents = fileContents;
            _fileName = fileName;
            _contentType = contentType;
            _fileType = fileType;
            _cancellationToken = cancellationToken;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var saveAsName = _fileName;
            if (_fileName.Contains('/'))
                saveAsName = _fileName.Substring(_fileName.IndexOf('/') + 1);

            var contentDisposition = new ContentDisposition()
            {
                Inline = true,
                FileName = saveAsName
            };


            var content = WriteBody(httpContext);

            httpContext.Response.ContentType = "text/html";
            httpContext.Response.Headers.ContentDisposition = contentDisposition.ToString();
            httpContext.Response.ContentLength = content.Length;

            return httpContext.Response.WriteAsync(content, _cancellationToken);
        }

        private string WriteBody(HttpContext httpContext)
        {
            if (_fileType.Equals(FileExtensions.MSG, StringComparison.InvariantCultureIgnoreCase))
                return WriteMsgResponse(httpContext);
            else if (_fileType.Equals(FileExtensions.EML, StringComparison.InvariantCultureIgnoreCase))
                return WriteEmlResponse(httpContext);

            return string.Empty;
        }

        private string WriteMsgResponse(HttpContext httpContext) => throw new NotImplementedException();

        private string WriteEmlResponse(HttpContext httpContext)
        {
            var content = new StringBuilder();
            var ms = new MemoryStream(_fileContents);
            var eml = MsgReader.Mime.Message.Load(ms);

            var to = eml.Headers?.To?.Select(x => x.Address).ToList() ?? new List<string>();
            var cc = eml.Headers?.Cc?.Select(x => x.Address).ToList() ?? new List<string>();
            var bcc = eml.Headers?.Bcc?.Select(x => x.Address).ToList() ?? new List<string>();
            var subject = eml.Headers?.Subject ?? string.Empty;
            var body = string.Empty;
            if (eml.HtmlBody != null)
            {
                body = System.Text.Encoding.UTF8.GetString(eml.HtmlBody.Body);
            }
            else if (eml.TextBody != null)
            {
                body = System.Text.Encoding.UTF8.GetString(eml.TextBody.Body);
            }
            
            return WriteHtml(to, cc, bcc, subject, body);
        }

        private string WriteHtml(List<string> to, List<string> cc, List<string> bcc, string subject, string body)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html><head></head><body><table style='text-align:left; width: 100%'>");

            sb.AppendLine($"<tr><th style='width: 5%'>To</th><td>{string.Join(", ", to)}</td></tr>");
            if(cc.Count > 0)
                sb.AppendLine($"<tr><th>Cc</th><td>{string.Join(", ", cc)}</td></tr>");
            if(bcc.Count > 0)
                sb.AppendLine($"<tr><th>Bcc</th><td>{string.Join(", ", bcc)}</td></tr>");

            sb.AppendLine($"<tr><th>Subject</th><td>{subject}</td></tr>");
            sb.AppendLine($"<tr><td colspan='2' style='padding-top:30px'>{body}</td></th>");
            sb.AppendLine("</table></body></html>");

            return sb.ToString();
        }
    }

    class InlineFileResult : IResult
    {
        private readonly byte[] _fileContents;
        private readonly string _fileName;
        private readonly string _contentType;
        private readonly CancellationToken _cancellationToken;

        public InlineFileResult(byte[] fileContents, string fileName, string contentType, CancellationToken cancellationToken)
        {
            _fileContents = fileContents;
            _fileName = fileName;
            _contentType = contentType;
            _cancellationToken = cancellationToken;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var saveAsName = _fileName;
            if (_fileName.Contains('/'))
                saveAsName = _fileName.Substring(_fileName.IndexOf('/') + 1);

            var contentDisposition = new ContentDisposition()
            {
                Inline = true,
                FileName = saveAsName
            };

            httpContext.Response.ContentType = _contentType;
            httpContext.Response.Headers.ContentDisposition = contentDisposition.ToString();

            return httpContext.Response.BodyWriter.AsStream().WriteAsync(_fileContents, 0, _fileContents.Length, _cancellationToken);
        }
    }
}
