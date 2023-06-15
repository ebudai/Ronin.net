using Ronin.Grammar.Compound;
using Ronin.Language;
using System.Diagnostics.CodeAnalysis;

namespace Ronin;

[ExcludeFromCodeCoverage]
internal class Analyzer
{
    public Context Define(Definition scope) => new(scope, Context.Global, canBeNamed: true);

    public void Resolve()
    {

    }
}
