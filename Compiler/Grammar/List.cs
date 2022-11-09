using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class List : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        if (parser.Current is not OpenSquareBracket) return null;
        parser.Advance();
        if (parser.Current is not CloseSquareBracket) return null;
        parser.Advance();
        return new List { Source = parser.Commit(ref context) };
    }
}
