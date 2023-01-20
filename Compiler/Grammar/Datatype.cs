using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Datatype : Syntax, Compiler.IParsable<Datatype>
{
    public Modifiers Is { get; init; }
    public Identifier Identifier { get; init; }
    public List<Algebra> Algebra { get; init; }
    public Scope Body { get; init; }

    public static Datatype Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.Current is not Lexicon.Reserved.Datatype) return null;
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        List<Algebra> algebra = null;
        if (parser.Current is Assign)
        {
            parser.Advance();
            algebra = parser.ParseRepeating<Algebra>();
        }

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Datatype
        {
            Is = modifiers,
            Identifier = identifier,
            Algebra = algebra,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }

    /*public override string ToString() => Is + " " + Identifier + AlgebraString() + Body;
    
    private string AlgebraString() => Algebra.ToString(); //todo algebra transpile*/
}
