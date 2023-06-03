using Ronin.Grammar;

namespace Ronin.Language;

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