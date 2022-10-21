using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    private Error(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        int length = 0;
        while (length < parser.Length)
        {
            var lexeme = parser[length];
            if (lexeme is Symbol symbol && !symbol.CanBeUsedInNames) break;
            ++length;
        }
        return new Error(parser, length + 1);
    }
}
