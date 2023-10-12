using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public bool Exports(Scope scope = null)
    {
        scope ??= Global;

        Export module = null;

        for (int i = 0; i != scope.Count; ++i)
        {
            if (scope[i] is Scope child && Exports(child))
            {
                scope[i] = new Noop();
            }            
            
            if (scope[i] is not Export export) continue;
            
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