using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Trivia : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        while (parser[0] is Trivium) ++parser.Cursor;
        if (parser[0] is Terminal) ++parser.Cursor;
        return parser.Cursor == context.Cursor ? null : new Trivia() { Tokens = parser.GetTokens(ref context) };
    }
}
