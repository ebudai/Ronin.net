using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Import : Syntax, IParsable
{
    internal string[] Name { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        if (context[0] is not Lexicon.Reserved.Import) return null;

        Parser parser = context;
        ++parser.Cursor;

        var parsed = Grammar.Name.Parse(ref parser);
        if (parsed is Error or null) return parsed;

        if (parsed is not Name name || parser[0] is not Terminal) return Error.Parse(ref parser);

        ++parser.Cursor; // for Terminal

        return new Import { Name = name.Hierarchy, Tokens = parser.GetTokens(ref context) };
    }
}
