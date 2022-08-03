using System.Text;
using System.Text.RegularExpressions;

namespace Ronin.Builder;

internal static class Parser
{
    internal class Result
    {
        internal string Expected;
        
    }
    internal static void Parse(string text, ref int cursor, ref int line)
    {
        Rune.DecodeFromUtf16(text, out var rune, out var consumed);
        if (Rune.IsWhiteSpace(rune))
        {
            ParseWhitespace(text, ref cursor, ref line);
        }
        else if (text[0] is '"')
        {
            ParseTextLiteral(text, ref cursor, ref line);
        }
        else if (text[0] is '\'')
        {
            ParseCharLiteral(text, ref cursor, ref line);
        }
        else if (Rune.IsDigit(rune))
        {
            if (text.Length is > 4 && text[4] is '-')
            {
                ParseDateLiteral();
            }
            else if (text.Length > 1 && text[1] is ':')
            {
                return ParseTimeLiteral();
            }
            else if (text.Length > 2 && text[2] is ':')
            {
                return ParseTimeLiteral();
            }
            else
            {
                return ParseNumericLiteral();
            }
        }
        else if (text[0] is '$')
        {
            return ParseMoneyLiteral();
        }
        else if (text[0] is '{')
        {
            return ParseScope();
        }
        else if (text[0] is '(')
        {
            return ParseParameters();
        }
        else
        {
            return ParseIdentifier();
        }
    }

    private static void ParseWhitespace(string sourcecode, ref int cursor, ref int line)
    {
        var match = whitespace.Match(sourcecode, cursor);
        cursor += match.Length;
        line += match.CountNewlines();
    }

    private static void ParseTextLiteral(string sourcecode, ref int cursor, ref int line)
    {
        var match = textliteral.Match(sourcecode, cursor);
        cursor += match.Length;
        line += match.CountNewlines();
    }

    private static void ParseCharLiteral(string sourcecode, ref int cursor, ref int line)
    {
        var match = charliteral.Match(sourcecode, cursor);
        if (!match.Success) match = unicharliteral
        cursor += match.Length;
        line += match.CountNewlines();
    }

    private static readonly Regex whitespace = new(@"\s+", options);
    private static readonly Regex textliteral = new(@"""[^""\\]*(\\.[^""\\]*)*""", options);
    private static readonly Regex charliteral = new(@"'\\?.'", options);
    private static readonly Regex unicharliteral = new(@"'\\u[a-f0-9]{4}'", options | RegexOptions.IgnoreCase)

    private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline;

    private static int CountNewlines(this Match match) => match.Value.Count(c => c == '\n');
}
