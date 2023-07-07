using Ronin.Grammar;

namespace Ronin.Language;

internal class Function : Semantic
{
    public Datatype Returns { get; init; }
    public Context Definition { get; init; }
    
    protected internal Function() : base(null) { }

    public Function(FunctionDeclaration function, Context context) : base(function)
    {
        Returns = new UnresolvedDatatype(function.Returns, context);
        Definition = context.Define(function.Definition);
    }

    public class Constructed
    {
        public List<Result> Parameters { get; init; } = new();
    }
}

internal class UnresolvedFunction : Function
{
    public Reference Reference { get; init; }
    public Context Context { get; init; }

    public UnresolvedFunction(Reference reference, Context context)
    {
        Reference = reference;
        Context = context;
    }
}