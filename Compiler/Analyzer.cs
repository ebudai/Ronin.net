using Ronin.Grammar.Compound;
using Ronin.Language;

namespace Ronin;

internal class Analyzer
{
    public Context Define(Definition scope) => Context.Global.Define(scope, canBeNamed: true);

    public void Resolve()
    {

    }
}
