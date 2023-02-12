using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Whitespace : Trivium
{
    internal static Whitespace Lex(ref Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length && char.IsWhiteSpace(lexer[length])) ++length;
        if (length is 0) return null;
        return new Whitespace { Sourcecode = lexer.Commit(length) };
    }
}
