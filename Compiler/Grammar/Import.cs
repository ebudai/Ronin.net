using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Import : Syntax, IParsable
{
    public string[] Name { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        if (context.Current is not Lexicon.Reserved.Import) return null;

        Parser parser = context;
        
        parser.Advance();

        var name = Grammar.Name.Parse(ref parser) as Name;
        if (name is null) return name;

        if (parser.IsNotFinished)
        {
            if (parser.Current is not Semicolon) return Error.Parse(ref context);
        }

        return new Import { Name = name.Hierarchy, Source = parser.Commit(ref context) };
    }
}
