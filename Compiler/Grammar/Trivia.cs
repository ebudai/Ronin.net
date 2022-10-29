using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Trivia : Syntax, IParsable
{
    public static Syntax Parse(Parser parser)
    {
        int length = -1;
        while (++length < parser.Length)
        {
            if (parser[length] is Trivium) continue;
            if (parser[length] is Terminal)
            {
                ++length;
                break;
            }
            return null;
        }
        return new Trivia() { Tokens = parser[..length] };
    }
}
