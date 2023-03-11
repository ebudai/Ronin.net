using Ronin.Grammar;

namespace Ronin.Language;

internal abstract class Semantics
{
    public List<Error> Errors { get; } = new();
    public Syntax Source { get; init; }
}
