using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable
{
    internal string[] Name { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        if (context.Current is not Lexicon.Reserved.PartOf) return null;

        Parser parser = context;
        
        parser.Advance();

        var name = Grammar.Name.Parse(ref parser) as Name;
        if (name is null) return name;

        if (parser.IsNotFinished)
        {
            if (parser.Current is not Semicolon) return ExpectedSemicolon.Parse(ref context);
        }

        return new PartOf { Name = name.Hierarchy, Source = parser.Commit(ref context) };
    }
}