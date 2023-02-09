using System.Text;

namespace KnowledgeMining.UI.Helpers
{
    public class Base64Helper
    {
        public static string UrlTokenToBase64(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            int len = input.Length;
            if (len < 1)
                return string.Empty;

            int numPadChars = (int)input[len - 1] - (int)'0';
            if (numPadChars < 0 || numPadChars > 10)
                return string.Empty;


            char[] base64Chars = new char[len - 1 + numPadChars];

            for (int iter = 0; iter < len - 1; iter++)
            {
                char c = input[iter];

                switch (c)
                {
                    case '-':
                        base64Chars[iter] = '+';
                        break;

                    case '_':
                        base64Chars[iter] = '/';
                        break;

                    default:
                        base64Chars[iter] = c;
                        break;
                }
            }

            for (int iter = len - 1; iter < base64Chars.Length; iter++)
            {
                base64Chars[iter] = '=';
            }

            return new string(base64Chars);
        }

        public static string DecodeToString(string input)
        {
            _ = input ?? throw new ArgumentException("input");

            //check if input is encoded base64
            if (Convert.TryFromBase64String(input,
                new Span<byte>(new byte[input.Length]),
                    out _))
            {
                var data = Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(data);
            }

            return input;
        }
    }
}
