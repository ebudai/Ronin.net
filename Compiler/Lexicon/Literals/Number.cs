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

        var match = NumbersWithCommas().Match(number);
        if (match.Success) return new Number(lexer, match.Length);

        match = NumbersWithoutCommas().Match(number);
        return new Number(lexer, match.Length);
    }

    [GeneratedRegex(@"\d+([.]\d+)?", options)]
    private static partial Regex NumbersWithoutCommas();
 
    [GeneratedRegex(@"\d{1,3}(,\d{3})+([.]\d+)?", options)]
    private static partial Regex NumbersWithCommas();

    private const RegexOptions options = RegexOptions.Compiled | RegexOptions.Singleline;
}
