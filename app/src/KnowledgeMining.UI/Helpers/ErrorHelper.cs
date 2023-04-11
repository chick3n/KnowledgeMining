using KnowledgeMining.Domain.Entities;

namespace KnowledgeMining.UI.Helpers
{
    public class ErrorHelper
    {
        public struct Error
        {
            public int Code { get; set; }
            public string Value { get; set; }
        }

        public static List<Error> GetErrorsFromMetadata(string errorMetadata)
        {
            var errors = new List<Error>();

            foreach (string err in errorMetadata.Split(';'))
            {
                if (err != "")
                {
                    var errorComponents = err.Split(':');

                    Error error = new Error();
                    error.Code = int.Parse(errorComponents[0]);
                    error.Value = errorComponents.Length > 1 ? errorComponents[1] : string.Empty;                 

                    errors.Add(error);
                }
            }

            return errors;
        }

        public static string GetErrorName(Error error, IndexItem indexItem)
        {
            return indexItem.DeserializedErrorHints[error.Code.ToString()]["name"].ToString();
        }

        public static string GetErrorHint(Error error, IndexItem indexItem)
        {
            return indexItem.DeserializedErrorHints[error.Code.ToString()]["hint"].ToString();
        }
    }
}
