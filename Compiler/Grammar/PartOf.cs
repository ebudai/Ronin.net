using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable
{
    internal string[] Name { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        if (context[0] is not Lexicon.Reserved.PartOf) return null;

        Parser parser = context;
        
        ++parser.Cursor;

        var name = Grammar.Name.Parse(ref parser) as Name;
        if (name is null) return name;

        if (parser.IsNotEmpty)
        {
            if (parser[0] is not Terminal) return Error.Parse(ref context);

            ++parser.Cursor; // for Terminal
        }

        return new PartOf { Name = name.Hierarchy, Tokens = parser.GetTokens(ref context) };
    }
}