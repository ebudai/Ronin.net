using Ronin.Compiler;
using Ronin.Grammar.Declaration;

namespace Ronin.Grammar;

internal class Statement : Syntax, IParsable<Statement>
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Hierarchy.Parse(ref parser)
            ?? Datum.Parse(ref parser)
            ?? Function.Parse(ref parser)
            ?? Datatype.Parse(ref parser)
            ?? Assignment.Parse(ref parser)
            ?? Reference.Parse(ref parser);

        if (syntax is not null) context = parser;

        return syntax;
    }

    public static Statement FromSyntax(Syntax syntax) => syntax switch 
    {
        Hierarchy hierarchy => new() { _storage = hierarchy },
        Datum datum => new() { _storage = datum },
        Function function => new() { _storage = function },
        Datatype datatype => new() { _storage = datatype },
        Assignment assignment => new() { _storage = assignment },
        Reference reference => new() { _storage = reference },
        _ => null,
    };

    public static implicit operator Hierarchy(Statement statement) => statement._storage as Hierarchy;
    public static implicit operator Datum(Statement statement) => statement._storage as Datum;
    public static implicit operator Function(Statement statement) => statement._storage as Function;
    public static implicit operator Datatype(Statement statement) => statement._storage as Datatype;
    public static implicit operator Assignment(Statement statement) => statement._storage as Assignment;
    public static implicit operator Reference(Statement statement) => statement._storage as Reference;

    private object _storage;
}
