using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Imports(Module module)
    {
        foreach (var subscope in module.Scopes)
        {
            Imports(subscope, module);
        }
        foreach (var submodule in module.Modules.Values)
        {
            Imports(submodule);
        }
    }

    private void Imports(Scope scope, Module parent)
    {
        for (int i = 0; i != scope.Imports.Count; ++i)
        {
            if (scope.Imports[i].Module is Module.Unresolved unresolved)
            {
                var import = parent[unresolved.Name];
                if (import is null)
                {
                    Errors.Add(new MissingModuleError(unresolved.Name));
                    continue;
                }
                scope.Imports[i].Module = import;
            }
        }
    }

    public class MissingModuleError : IError
    {
        public MissingModuleError(Name name) => ((IError)this).ExtractTokens(name.Tokens);

        public string Reason { get; } = "could not locate module";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
