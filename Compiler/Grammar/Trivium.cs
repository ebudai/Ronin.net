using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Symbols;

namespace Ronin.Grammar;

internal class Trivium : Syntax, IParsable
{
    public Trivium(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        int length = -1;
        while (++length < parser.Length)
        {
            if (parser[length] is Whitespace or Comment) continue;
            if (parser[length] is Terminal)
            {
                ++length;
                break;
            }
            return null;
        }
        return new Trivium(parser, length);
    }
}
