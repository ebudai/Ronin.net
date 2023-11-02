using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Imports(Scope scope)
    {
        for (int i = 0; i != scope.Imports.Count; ++i) 
        {
            if (scope.Imports[i] is Module.Unresolved unresolved)
            {
                var import = Global[unresolved.Name];
                if (import is null)
                {
                    Errors.Add(new MissingModuleError(unresolved.Name));
                    continue;
                }
                scope.Imports[i] = import;
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
