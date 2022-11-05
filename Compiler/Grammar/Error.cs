using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    internal List<Type> Expected { get; init; } = new();

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        while (parser.IsNotEmpty)
        {
            ref readonly var token = ref parser[0];
            ++parser.Cursor;
            if (token is Terminal or Separator or Close) break;            
        }
        return new Error { Tokens = parser.GetTokens(ref context) };
    }
}
