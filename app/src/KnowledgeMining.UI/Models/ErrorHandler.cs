using System.Collections.ObjectModel;

namespace KnowledgeMining.UI.Models
{
    public class ErrorHandler
    {
        private Dictionary<int, string> _error_code_to_name = new Dictionary<int, string>
        {
            { 1, "Failed To Import" },
            { 2, "Unknown File Type" },
            { 3, "Failed To Translate" },
            { 4, "Access Denied" },
            { 5, "Failed To Read File" },
            { 6, "Email Address Found" }
        };

        private Dictionary<string, int> _error_name_to_code;

        public ErrorHandler() {
            // Generate list of _error_name_to_code from the list of error codes to names
            _error_name_to_code = new Dictionary<string, int>();
            foreach (var key in _error_code_to_name.Keys)
            {
                _error_name_to_code.Add(_error_code_to_name[key], key);
            }
        }
    }
}
