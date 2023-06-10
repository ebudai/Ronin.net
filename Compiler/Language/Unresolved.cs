using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Unresolved : Semantic
{
    public Reference Reference { get; }

    public Unresolved(Reference reference, Syntax source)
    {
        Reference = reference;
        Source = source;
    }
}