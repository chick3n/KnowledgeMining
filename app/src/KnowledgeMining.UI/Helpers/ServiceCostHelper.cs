namespace KnowledgeMining.UI.Helpers
{
    public class ServiceCostHelper
    {
        public static string FromValue(string value)
        {
            value = value ?? string.Empty;
            value = value.Trim().ToLower();

            if (value == "1")
                return "$$$$";
            if (value == "2")
                return "$$$";
            if (value == "3")
                return "$$";

            return "$";
        }

        public static string ToValue(string value)
        {
            value = value ?? string.Empty;
            value = value.Trim().ToLower();

            if (value == "$$$$")
                return "1";
            if (value == "$$$")
                return "2";
            if (value == "$$")
                return "3";

            return "4";
        }
    }
}
