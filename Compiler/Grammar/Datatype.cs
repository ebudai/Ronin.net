using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Datatype : Syntax, IParsable
{
    public Modifiers Is { get; init; }
    public Identifier Identifier { get; init; }
    public List<Algebra> Algebra { get; init; }
    public Scope Body { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser) as Modifiers;

        if (parser.Current is not Lexicon.Reserved.Datatype) return null;
        parser.Advance();

        var identifier = Identifier.Parse(ref parser);
        if (identifier is Error or null) return identifier;

        List<Algebra> algebra = null;
        if (parser.Current is Assign)
        {
            parser.Advance();
            algebra = parser.Parse<Algebra>();
        }

        var body = Scope.Parse(ref parser);
        if (body is Error or null) return body;

        return new Datatype
        {
            Is = modifiers,
            Identifier = identifier as Identifier,
            Algebra = algebra,
            Body = body as Scope,
            Source = parser.Commit(ref context)
        };
    }

    /*public override string ToString() => Is + " " + Identifier + AlgebraString() + Body;
    
    private string AlgebraString() => Algebra.ToString(); //todo algebra transpile*/
}
