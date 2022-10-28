using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    internal List<Type> Expected { get; init; } = new();

    public static Syntax Parse(Parser parser)
    {
        parser.Reset(); 
        while (parser.IsNotEmpty)
        {
            ref var lexeme = ref parser[0];            
            if (lexeme is Symbol symbol && !symbol.CanBeUsedInNames) break;
            ++parser.Cursor;
        }
        return new Error { Tokens = parser.Tokens };
    }
}
