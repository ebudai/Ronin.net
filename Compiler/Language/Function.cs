using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function
{
    public Identifier Identifier { get; init; }

    public List<Datatype> Datatypes { get; } = new();
    public List<Datum> Parameters { get; } = new();
    public List<Datum> Data { get; } = new();
    public List<Function> Operations { get; } = new();

    public List<Instruction> Instructions { get; } = new();
}

internal class UnresolvedFunction : Function
{
    public Reference Reference { get; init; }
}