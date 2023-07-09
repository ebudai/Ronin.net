using Ronin.Grammar;

namespace Ronin.Language;

internal class Function : Semantic
{
    public Datatype Returns { get; init; }
    public Context Definition { get; init; }

    public Function(FunctionDeclaration function, Context context) : base(function)
    {
        Returns = new UnresolvedDatatype(function.Returns, context);
        Definition = context.Define(function.Definition);
    }
}