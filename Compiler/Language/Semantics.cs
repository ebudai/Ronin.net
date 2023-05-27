using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal abstract class Semantics
{
    public Semantics Parent { get; init; }
    public List<Module> Imports { get; init; } = new();
    public Dictionary<Identifier, Datatype> Datatypes { get; init; } = new();
    public Dictionary<Identifier, Function> Functions { get; init; } = new();
    public Dictionary<Identifier, Datum> Data { get; init; } = new();
    public List<Error> Errors { get; } = new();
    public Syntax Source { get; init; }

    public Error Add(KeyValuePair<Identifier, Function> function)
    {
        throw new NotImplementedException();
    }

    public Error Add(KeyValuePair<Identifier, Datatype> datatype)
    {
        throw new NotImplementedException();
    }

    public Error Add(KeyValuePair<Identifier, Datum> datum)
    {
        throw new NotImplementedException();
    }

    public Semantics Find(Identifier identifier)
    {
        throw new NotImplementedException();
    }

    public static Semantics Declare<T>(Syntax syntax) where T : Semantics, new()
    {
        T semantics = new() { Source = syntax };



        return semantics;
    }
}
