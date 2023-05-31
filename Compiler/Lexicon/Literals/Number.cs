// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Text.RegularExpressions;

namespace Ronin.Lexicon.Literals;

internal partial class Number : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || char.IsDigit(lexer[0]) is false) return null;

        int length = 1;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];

            if (char.IsWhiteSpace(c)) break;
            if (char.IsDigit(c) is false && c is not ',' and not '.') break;
        }

        var number = lexer[..length].ToString();

        var match = NumbersWithCommas().Match(number);
        if (match.Success) return new Number { sourcecode = lexer.Commit(match.Length) };

        match = NumbersWithoutCommas().Match(number);
        return new Number { sourcecode = lexer.Commit(match.Length) };
    }

    [GeneratedRegex("[0-9]+([.][0-9]+)?", RegexOptions.Compiled | RegexOptions.Singleline)] 
    private static partial Regex NumbersWithoutCommas();
    
    [GeneratedRegex("[0-9]{1,3}(,[0-9]{3})+([.][0-9]+)?", RegexOptions.Compiled | RegexOptions.Singleline)] 
    private static partial Regex NumbersWithCommas();
}
