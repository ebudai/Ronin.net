using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Url : Literal
{
    public static new Url Lex(ref Lexer lexer)
    {
        if (lexer.Length is < 5) return null;

        // get scheme
        int length = 0;
        while (length < lexer.Length && char.IsLetter(lexer[length])) ++length;
        if (length == lexer.Length) return null;

        if (length + 4 >= lexer.Length || lexer[length] is not ':' || lexer[length + 1] is not '/' || lexer[length + 2] is not '/') return null;

        length += 3;
        while (length < lexer.Length && IsValidUrlCharacter(lexer[length])) ++length;

        return new Url { sourcecode = lexer.Commit(length) };
    }

    private static bool IsValidUrlCharacter(char value) => char.IsLetterOrDigit(value) || value is '~' or '*' or '(' or ')' or '.' or '-' or '_' or '/';
}
