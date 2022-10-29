using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Import : Syntax, IParsable
{
    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser[0] is not Lexicon.Reserved.Import) return null;

        ++parser.Cursor;

        var parsed = Grammar.Name.Parse(parser);
        if (parsed is Error or null) return parsed;

        if (parsed is not Name name || parser[0] is not Terminal) return Error.Parse(parser);

        ++parser.Cursor; // for Terminal

        return new Import { Name = name.Hierarchy, Tokens = parser.Tokens };
    }
}
