using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Delimiter;

namespace Ronin.Grammar;

internal class Trivium : Syntax, IParsable
{
    public Trivium(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        int length = -1;
        while (++length < parser.Length)
        {
            if (parser[length] is Whitespace or Comment or Terminal) continue;
            return null;
        }
        return new Trivium(parser, length);
    }
}
