using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    internal List<Type> Expected { get; init; } = new();

    public static Syntax Parse(Parser parser)
    {
        int length = 0;
        while (length < parser.Length)
        {
            var lexeme = parser[length];
            if (lexeme is Symbol symbol && !symbol.CanBeUsedInNames) break;
            ++length;
        }
        return new Error { Tokens = parser[..length] };
    }
}
