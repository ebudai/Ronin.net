using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal abstract class Semantics
{
    public List<Error> Errors { get; } = new();
    public Syntax Source { get; init; }
}
