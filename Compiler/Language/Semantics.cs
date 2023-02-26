using Ronin.Grammar;

namespace Ronin.Language;

internal abstract class Semantics
{
    public Semantics(Semantics parent) => Parent = parent;

    public Semantics Parent { get; }
    public List<Error> Errors { get; } = new();
    public Syntax Source { get; init; }
}
