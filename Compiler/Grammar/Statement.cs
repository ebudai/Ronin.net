using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Statement : Syntax, IParsable
{
    public Syntax Syntax { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Hierarchy.Parse(ref parser)
            ?? Datum.Parse(ref parser)
            ?? Function.Parse(ref parser)
            ?? Datatype.Parse(ref parser)
            ?? Assignment.Parse(ref parser)
            ?? Reference.Parse(ref parser);

        if (syntax is Error or null) return syntax;

        return new Statement { Syntax = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Hierarchy(Statement statement) => statement.Syntax as Hierarchy;
    public static implicit operator Datum(Statement statement) => statement.Syntax as Datum;
    public static implicit operator Function(Statement statement) => statement.Syntax as Function;
    public static implicit operator Datatype(Statement statement) => statement.Syntax as Datatype;
    public static implicit operator Assignment(Statement statement) => statement.Syntax as Assignment;
    public static implicit operator Reference(Statement statement) => statement.Syntax as Reference;

    public override string ToString() => Syntax.ToString();
}
