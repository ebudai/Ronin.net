using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public bool Exports(IContext context = null)
    {
        context ??= Global;

        Export module = null;

        for (int i = 0; i != context.Count; ++i)
        {
            if (context[i] is Scope child && Exports(child))
            {
                context[i] = null;
            }       
            
            if (context[i] is not Export export) continue;
            
            if (module is not null)
            {
                Errors.Add(new MultipleModuleNameError(module, export));
                continue;
            }

            module = export;
        }

        if (module is null) return false;

        if (context is Scope scope)
        {
            Global.GetOrAdd(module.Name).Add(scope);
        }

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