using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Word : Token
{
    internal Word(Lexer lexer, int length) : base(lexer, length) { }

    internal static Word Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (char.IsNumber(lexer[0])) return null;

        var length = 0;
        while (length < lexer.Length 
            && !char.IsWhiteSpace(lexer[length])
            && !Symbol.IsSymbol(lexer, length)) ++length;

        return length is 0 ? null : new Word(lexer, length);
    }
}