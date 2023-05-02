using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Module
{
    public Identifier Identifier { get; init; }

    public Datatype ReturnDatatype { get; init; }

    public Function(Grammar.Function function)
    {
        Identifier = function.Identifier;
    }
}

[ExcludeFromCodeCoverage]
internal class FunctionCannotJoinNamedScope : Error { }

[ExcludeFromCodeCoverage]
internal class FunctionAlreadyExists : Error { }