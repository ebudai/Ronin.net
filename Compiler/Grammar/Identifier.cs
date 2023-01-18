using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Identifier : Syntax, IParsable
{
    public List<Component> Components { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        var components = parser.Parse<Component>();

        if (components.Count is 0) return null;

        return new Identifier { Components = components, Source = parser.Commit(ref context) };
    }

    //public override string ToString() => string.Join("", Components);

    public class Component : Syntax, IParsable
    {
        public Syntax Syntax { get; init; }

        public static Syntax Parse(ref Parser context)
        {
            Parser parser = context;
            
            Syntax syntax = Name.Parse(ref parser) ?? Parameters.Parse(ref parser);
            if (syntax is Error or null) return syntax;

            return new Component { Syntax = syntax, Source = parser.Commit(ref context) };
        }        
    }
}
