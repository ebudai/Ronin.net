using Ronin.Grammar;

namespace Ronin.Language;

internal class Semantic
{
    public Syntax Source { get; init; }
    public List<Error> Errors { get; } = new();

    public Semantic() { }

    public Semantic(Syntax source) => Source = source;
}
