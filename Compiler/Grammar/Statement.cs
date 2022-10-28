using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Statement : Syntax, IParsable
{
    internal Value Body { get; private init; }

    public static Syntax Parse(Parser context)
    {
        Parser parser = new(context);
        var value = Value.Parse(parser);
        if (value is Error or null) return value;

        if (parser[0] is not Terminal) return null;

        ++parser.Cursor;

        return new Statement { Body = value, Tokens = context[..parser.Location] };
    }
}
