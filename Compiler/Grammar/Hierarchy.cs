using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

internal class Hierarchy : Syntax, IParsable
{
    public Keyword Direction { get; init; }
    public Name Name { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Keyword direction = context.Current is PartOf or Import ? context.Current as Keyword : null;
        if (direction is null) return null;

        Parser parser = context;
        parser.Advance();

        var name = Name.Parse(ref parser);
        if (name is Error or null) return null;

        return new Hierarchy 
        {
            Direction = direction,
            Name = name as Name,             
            Source = parser.Commit(ref context) 
        };
    }

    /*public override string ToString()
    {
        var code = Direction switch
        {
            PartOf => "namespace ",
            Import => "using ",
            _ => string.Empty,
        };
        return code + string.Join(".", Name);
    }*/
}