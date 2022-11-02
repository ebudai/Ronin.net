using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Trivia : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        ref var token = ref parser[0];
        while (parser.IsNotEmpty && token is Trivium) ++parser.Cursor;
        if (token is Terminal) ++parser.Cursor;
        return new Trivia() { Tokens = parser.GetTokens(ref context) };
    }
}
