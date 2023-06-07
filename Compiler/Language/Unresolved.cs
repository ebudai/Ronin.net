using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Unresolved : Semantic
{
    public Reference Reference { get; }

    public Unresolved(Reference reference, Context context, Syntax source)
    {
        Reference = reference;
        Context = context;
        Source = source;
    }
}