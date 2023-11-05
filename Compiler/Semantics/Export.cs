using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public bool Exports(Scope scope)
    {
        Export module = null;

        for (int i = 0; i != scope.Statements.Count; ++i)
        {
            if (scope.Statements[i] is Scope child && Exports(child))
            {
                scope.Statements[i] = null;
            }       
            
            if (scope.Statements[i] is not Export export) continue;
            
            if (module is not null)
            {
                Errors.Add(new MultipleModuleNameError(module, export));
                continue;
            }

            module = export;
        }

        if (module is null) return false;

        Global.GetOrAdd(module.Name).Add(scope);
        
        return true;
    }

    public class MultipleModuleNameError : IError
    {
        public MultipleModuleNameError(Export existing, Export duplicate)
        {
            Tokens = ((IError)this).ExtractTokens(existing.Keyword, existing.Name.Tokens, duplicate.Keyword, duplicate.Name.Tokens);
        }

        public string Reason { get; } = "multiple module names";
        public ReadOnlyMemory<Token> Tokens { get; }
    }
}