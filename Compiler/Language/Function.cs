using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Semantic
{
    public Datatype Returns { get; }
    public Context Definition { get; }
    
    protected internal Function() : base(null) { }

    public Function(FunctionDeclaration function, Context context) : base(function)
    {
        Returns = new UnresolvedDatatype(function.Returns, context);
        Definition = new(function.Definition, context);
    }

    public class Constructed
    {
        public List<Result> Parameters { get; init; } = new();
    }
}

[ExcludeFromCodeCoverage]
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