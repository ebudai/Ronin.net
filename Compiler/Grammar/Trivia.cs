using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Trivia : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        while (parser[0] is Trivium) parser.Advance();
        if (parser[0] is Semicolon) parser.Advance();
        return parser.Current == context.Current ? null : new Trivia { Source = parser.Commit(ref context) };
    }
}
