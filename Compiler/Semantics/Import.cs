using Ronin.Grammar;

namespace Ronin.Semantics;

internal partial class Resolver
{
    public void Imports(Scope scope)
    {
        for (int i = 0; i != scope.Imports.Count; ++i) 
        {
            if (scope.Imports[i] is Module.Unresolved unresolved)
            {
                scope.Imports[i] = Global.Get(unresolved.Name);
            }
        }
    }
}
