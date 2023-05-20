using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Result : Semantics
{
    public List<string> Values { get; init; } = new();
    public Datatype Datatype { get; init; }
}
