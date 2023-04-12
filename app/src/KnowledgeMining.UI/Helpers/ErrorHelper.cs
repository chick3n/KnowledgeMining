using Azure.Storage.Blobs.Models;
using KnowledgeMining.Domain.Entities;

namespace KnowledgeMining.UI.Helpers
{
    /// <summary>
    /// Helper class to process, manage, and reference document error information.
    /// </summary>
    public class ErrorHelper
    {
        public struct Error
        {
            public int Code { get; set; }
            public string Value { get; set; }
        }

        /// <summary>
        /// Converts a string value of document errors to a list of Error objects.
        /// </summary>
        /// <param name="errorMetadata">String of errors formated by document ingestion</param>
        /// <returns>List of Error objects</returns>
        public static List<Error> GetErrorsFromMetadata(string errorMetadata)
        {
            var errors = new List<Error>();

            foreach (string err in errorMetadata.Split("; "))
            {
                if (err != "")
                {
                    var errorComponents = err.Split(": ");

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

        public static List<int> GetValidErrorCodes(IndexItem indexItem)
        {
            List<int> validErrorCodes = new List<int>();

            foreach (var key in indexItem.DeserializedErrorHints.Keys)
            {
                validErrorCodes.Add(int.Parse(key));
            }

            return validErrorCodes;
        }
    }
}
