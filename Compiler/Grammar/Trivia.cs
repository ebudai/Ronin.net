using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Trivia : Syntax, Compiler.IParsable<Trivia>
{
    public static Trivia Parse(ref Parser context)
    {
        Parser parser = context;
        while (parser[0] is Trivium) parser.Advance();
        if (parser[0] is Terminal) parser.Advance();
        return parser.Current == context.Current ? null : new Trivia { Source = parser.Commit(ref context) };
    }
}
