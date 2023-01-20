using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

internal class Modifiers : Syntax, Compiler.IParsable<Modifiers>
{
    internal bool Persistent { get; private init; }
    internal bool Compiled { get; private init; }
    internal bool Shared { get; private init; }
    internal bool Optional { get; private init; }

    public static Modifiers Parse(ref Parser context)
    {
        bool persistent = false;
        bool compiled = false;
        bool shared = false;
        bool optional = false;

        Parser parser = context;
        
        while (parser.IsNotFinished)
        {
            var modifier = parser.Current;

            // the point of these is to break if you encounter a keyword twice
            // the 2nd time it's part of the name, which is parsed somewhere else
            if (modifier is Compiled && compiled is not true && (compiled = true)) parser.Advance();
            else if (modifier is Persistent && persistent is not true && (persistent = true)) parser.Advance();
            else if (modifier is Shared && shared is not true && (shared = true)) parser.Advance();
            else if (modifier is Optional && optional is not true && (optional = true)) parser.Advance();
            else break;
        }

        return new Modifiers
        {
            Persistent = persistent,
            Compiled = compiled,
            Shared = shared,
            Optional = optional,
            Source = parser.Commit(ref context)
        };
    }

    //public override string ToString() => Shared ? "static" : string.Empty;
}
