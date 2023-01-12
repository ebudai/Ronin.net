using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Statement : Syntax, Compiler.IParsable<Statement>
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Hierarchy.Parse(ref parser)
            ?? Datum.Declaration.Parse(ref parser)
            ?? Function.Declaration.Parse(ref parser)
            ?? Datatype.Declaration.Parse(ref parser)
            ?? Assignment.Parse(ref parser)
            ?? Reference.Parse(ref parser);

        if (syntax is not null) context = parser;

        return syntax;
    }

    public static Statement FromSyntax(Syntax syntax) => syntax switch 
    {
        Hierarchy hierarchy => new() { _storage = hierarchy },
        Datum.Declaration datum => new() { _storage = datum },
        Function.Declaration function => new() { _storage = function },
        Datatype.Declaration datatype => new() { _storage = datatype },
        Assignment assignment => new() { _storage = assignment },
        Reference reference => new() { _storage = reference },
        _ => null,
    };

    public static implicit operator Hierarchy(Statement statement) => statement._storage as Hierarchy;
    public static implicit operator Datum.Declaration(Statement statement) => statement._storage as Datum.Declaration;
    public static implicit operator Function.Declaration(Statement statement) => statement._storage as Function.Declaration;
    public static implicit operator Datatype.Declaration(Statement statement) => statement._storage as Datatype.Declaration;
    public static implicit operator Assignment(Statement statement) => statement._storage as Assignment;
    public static implicit operator Reference(Statement statement) => statement._storage as Reference;

    private object _storage;
}
