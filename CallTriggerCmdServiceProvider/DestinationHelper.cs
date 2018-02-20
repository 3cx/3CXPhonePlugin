using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCX.CallTriggerCmd
{
    public static class DestinationHelper
    {
        static readonly string[] prefixes = new string[] { "tcxclicktocall:", "3cxclicktocall:", "tel:", "callto:", "sip:", "tcxcallto:" };

        public static string Normalize(string input)
        {
            foreach (var prefix in prefixes)
                // Strip letters for tel: protocol
                if (input.StartsWith(prefix))
                    input = input.Substring(prefix.Length);
            if (input.StartsWith("//"))
                input = input.Substring(2);

            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (Char.IsLetter(c))
                {
                    switch (new string(c, 1).ToUpper())
                    {
                        case "A": // fall down
                        case "B": // fall down
                        case "C": sb.Append('2'); break;
                        case "D": // fall down
                        case "E": // fall down
                        case "F": sb.Append('3'); break;
                        case "G": // fall down
                        case "H": // fall down
                        case "I": sb.Append('4'); break;
                        case "J": // fall down
                        case "K": // fall down
                        case "L": sb.Append('5'); break;
                        case "M": // fall down
                        case "N": // fall down
                        case "O": sb.Append('6'); break;
                        case "P": // fall down
                        case "Q": // fall down
                        case "R": // fall down
                        case "S": sb.Append('7'); break;
                        case "T": // fall down
                        case "U": // fall down
                        case "V": sb.Append('8'); break;
                        case "W": // fall down
                        case "X": // fall down
                        case "Y": // fall down
                        case "Z": sb.Append('9'); break;
                    }
                }
                else if (Char.IsDigit(c) || c == '+' || c == '#' || c == '*')
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
