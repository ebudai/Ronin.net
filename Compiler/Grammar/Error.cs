using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    internal List<Type> Expected { get; init; } = new();

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        parser.Cursor = 0;
        while (parser.IsNotEmpty)
        {
            if (parser[0] is Symbol symbol && symbol.CanBeUsedInNames is not true) break;
            ++parser.Cursor;
        }
        return new Error { Tokens = parser.GetTokens(ref context) };
    }
}
