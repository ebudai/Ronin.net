using Ronin.Compiler;

namespace Ronin.Token;

internal class Keyword : Token
{
    internal Keyword(Lexer lexer, int length) : base(lexer, length) { }

    internal static Token Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        foreach (var word in Enum.GetValues<Word>())
        {
            var name = Enum.GetName(word).Replace("_", " ");
            if (lexer.StartsWith(name))
            {
                if (lexer.Length <= name.Length)
                {
                    lexer.Error = "unterminated declaration";
                    return null;
                }

                if (char.IsWhiteSpace(lexer[name.Length]) || Symbol.IsSymbol(lexer, name.Length)) return new Keyword(lexer, name.Length);
            }
        }
        return null;
    }

    internal enum Word
    {
        datatype,
        function,
        var,
        constant,
        reactive,
        compiled,
        persistent,
        shared,
        optional,
        part_of,
        import,
        @return,
    }
}
