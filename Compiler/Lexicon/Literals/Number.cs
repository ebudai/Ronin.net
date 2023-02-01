using Ronin.Compiler;
using System.Text.RegularExpressions;

namespace Ronin.Lexicon.Literals;

internal partial class Number : Literal
{
    private Number(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Token Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || char.IsNumber(lexer[0]) is false) return null;

        int length = 1;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];

            if (char.IsWhiteSpace(c)) break;
            if (char.IsNumber(c) is false && c is not ',' and not '.') break;
        }

        var number = lexer[..length].Span.ToString();

        var match = commasregex.Match(number);
        if (match.Success) return new Number(lexer, match.Length);

        match = nocommasregex.Match(number);
        return new Number(lexer, match.Length);
    }

    private static readonly Regex nocommasregex = NoCommasRegex();
    private static readonly Regex commasregex = CommasRegex();

    [GeneratedRegex(@"\d+([.]\d+)?", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex NoCommasRegex();
 
    [GeneratedRegex(@"\d{1,3}(,\d{3})+([.]\d+)?", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex CommasRegex();
}
