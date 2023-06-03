using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Semantic
{
    public Datatype Returns { get; init; }
    public Context Definition { get; init; }
    public List<Instruction> Instructions { get; init; } = new();    
}

[ExcludeFromCodeCoverage]
internal class UnresolvedFunction : Function
{
    public new Unresolved Returns { get; init; }

    public UnresolvedFunction(FunctionDeclaration function, Context context)
    {
        Context = context;
        Source = function;
        Returns = new Unresolved(function.Returns, context, function);
        Definition = new Context(function.Definition, context, false, Instructions);
    }
}