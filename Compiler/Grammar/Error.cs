using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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
            if (parser[0] is Terminal or Separator or Close) break;
            ++parser.Cursor;
        }
        return new Error { Tokens = parser.GetTokens(ref context) };
    }
}
