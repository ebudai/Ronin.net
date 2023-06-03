using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Semantic
{
    public Context Context { get; init; } = Context.Global;
    public List<Error> Errors { get; } = new();
    public Syntax Source { get; init; }
}
