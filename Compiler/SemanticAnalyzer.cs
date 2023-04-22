using Ronin.Grammar.Compound;
using Ronin.Language;

namespace Ronin;

internal class SemanticAnalyzer
{
    public static Module Analyze(Scope scope)
    {
        return UnresolvedModule.From(scope);
    }
}
