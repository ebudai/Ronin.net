using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable
{
    internal string[] Name { get; init; }

    public static Syntax Parse(Parser context)
    {
        if (context[0] is not Lexicon.Reserved.PartOf) return null;

        Parser parser = new(context, 1);
        var parsed = Grammar.Name.Parse(parser);

        if (parsed is Error error) return error;

        if (parsed is Name name && parser[0] is Terminal)
        {
            ++parser.Cursor; // for Terminal
            var tokens = context[..parser.Cursor];
            context.Cursor = context.Cursor;
            return new PartOf { Name = name.Hierarchy, Tokens = tokens };
        }

        return Error.Parse(parser);
    }
}
  