using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

internal class Modifiers : Syntax, IParsable
{
    internal bool Persistent { get; private init; }
    internal bool Compiled { get; private init; }
    internal bool Shared { get; private init; }
    internal bool Optional { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        bool persistent = false;
        bool compiled = false;
        bool shared = false;
        bool optional = false;

        Parser parser = context;

        parser.AdvancePastTrivia();

        while (parser.IsNotEmpty)
        {
            ref readonly var modifier = ref parser[0];
            ++parser.Cursor;

            if (modifier is Trivium) continue;

            // the point of these is to break if you encounter a keyword twice
            // the 2nd time it's part of the name, whic is parsed somewhere else
            if (modifier is Compiled && compiled is not true && (compiled = true)) continue;
            if (modifier is Persistent && persistent is not true && (persistent = true)) continue;
            if (modifier is Shared && shared is not true && (shared = true)) continue;
            if (modifier is Optional && optional is not true && (optional = true)) continue;

            --parser.Cursor;
            break;
        }

        return new Modifiers
        {
            Persistent = persistent,
            Compiled = compiled,
            Shared = shared,
            Optional = optional,
            Tokens = parser.GetTokens(ref context),
        };
    }
}
