using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Context
{
    public Datatype ReturnDatatype { get; init; }

    public Function(Grammar.Function function)
    {

    }
}

[ExcludeFromCodeCoverage]
internal class FunctionCannotJoinNamedScope : Error { }

[ExcludeFromCodeCoverage]
internal class FunctionAlreadyExists : Error { }