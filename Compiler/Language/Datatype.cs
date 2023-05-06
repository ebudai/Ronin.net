using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datatype : Context
{
    public bool IsOptional { get; init; }

    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public Datatype() { }

    public static Datatype ForwardDeclare(Grammar.Datatype datatype)
    {
        throw new NotImplementedException();
    }
}

[ExcludeFromCodeCoverage]
internal class DatatypeCannotJoinNamedScope : Error { }

[ExcludeFromCodeCoverage]
internal class DatatypeDefinitionCannotContain<T> : Error where T : Syntax { }

[ExcludeFromCodeCoverage]
internal class DatatypeAlreadyExists : Error { }