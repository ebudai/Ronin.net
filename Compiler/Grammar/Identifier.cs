using Ronin.Compiler;
using Ronin.Grammar.Unions;

namespace Ronin.Grammar;

internal class Identifier : Syntax, IParsable
{
    public List<IdentifierComponent> Components { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<IdentifierComponent> components = new();
        
        while (parser.IsNotFinished)
        {
            var syntax = IdentifierComponent.Parse(ref parser);
            if (syntax is Error) return syntax;
            if (syntax is null) break;
            components.Add(syntax);
        }

        if (components.Count is 0) return null;

        return new Identifier { Components = components, Source = parser.Commit(ref context) };
    }

    
}
