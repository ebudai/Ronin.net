using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Semantic
{
    public Syntax Source { get; }
    public List<Error> Errors { get; } = new();

    public Semantic(Syntax source)
    {
        Source = source;
    }
}
