using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Context : Semantics
{
    public Dictionary<Identifier, Datatype> Datatypes { get; init; } = new();
    public Dictionary<Identifier, Function> Functions { get; init; } = new();
    public Dictionary<Identifier, Datum> Data { get; init; } = new();

    public Semantics Find(Identifier identifier)
    {
        throw new NotImplementedException();
    }

    public Semantics Find(Reference reference)
    {
        throw new NotImplementedException();
    }
}