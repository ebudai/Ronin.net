using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Statement : Syntax, Compiler.IParsable<Statement>
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Hierarchy.Parse(ref parser)
            ?? Declaration.Datum.Parse(ref parser)
            ?? Declaration.Function.Parse(ref parser)
            ?? Declaration.Datatype.Parse(ref parser)
            ?? Assignment.Parse(ref parser)
            ?? Reference.Parse(ref parser);

        if (syntax is not null) context = parser;

        return syntax;
    }

    public static Statement FromSyntax(Syntax syntax) => syntax switch 
    {
        Hierarchy hierarchy => new() { _storage = hierarchy },
        Declaration.Datum datum => new() { _storage = datum },
        Declaration.Function function => new() { _storage = function },
        Declaration.Datatype datatype => new() { _storage = datatype },
        Assignment assignment => new() { _storage = assignment },
        Reference reference => new() { _storage = reference },
        _ => null,
    };

    public static implicit operator Hierarchy(Statement statement) => statement._storage as Hierarchy;
    public static implicit operator Declaration.Datum(Statement statement) => statement._storage as Declaration.Datum;
    public static implicit operator Declaration.Function(Statement statement) => statement._storage as Declaration.Function;
    public static implicit operator Declaration.Datatype(Statement statement) => statement._storage as Declaration.Datatype;
    public static implicit operator Assignment(Statement statement) => statement._storage as Assignment;
    public static implicit operator Reference(Statement statement) => statement._storage as Reference;

    private object _storage;
}
