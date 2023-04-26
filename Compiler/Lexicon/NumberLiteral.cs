// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Text.RegularExpressions;

namespace Ronin.Lexicon;

internal partial class NumberLiteral : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || char.IsNumber(lexer[0]) is false) return null;

        int length = 1;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];

            if (char.IsWhiteSpace(c)) break;
            if (char.IsNumber(c) is false && c is not ',' and not '.') break;
        }

        var number = lexer[..length].ToString();

        var match = NumbersWithCommas().Match(number);
        if (match.Success) return new NumberLiteral { sourcecode = lexer.Commit(match.Length) };

        match = NumbersWithoutCommas().Match(number);
        return new NumberLiteral { sourcecode = lexer.Commit(match.Length) };
    }

    [GeneratedRegex("\\d+([.]\\d+)?", RegexOptions.Compiled | RegexOptions.Singleline)] 
    private static partial Regex NumbersWithoutCommas();
    
    [GeneratedRegex("\\d{1,3}(,\\d{3})+([.]\\d+)?", RegexOptions.Compiled | RegexOptions.Singleline)] 
    private static partial Regex NumbersWithCommas();
}
