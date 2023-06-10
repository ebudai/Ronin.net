using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Context
{
    public Datatype Returns { get; init; }
    public List<Instruction> Instructions { get; init; } = new();    
}

[ExcludeFromCodeCoverage]
internal class UnresolvedFunction : Function
{
    public Unresolved UnresolvedReturns { get; init; }
    public new Context Context
    {
        get => base.Context;
        set
        {
            base.Context = value;
            UnresolvedReturns.Context = value;
        }
    }

    public UnresolvedFunction(FunctionDeclaration function)
    {
        Source = function;
        UnresolvedReturns = new Unresolved(function.Returns, function);
    }
}