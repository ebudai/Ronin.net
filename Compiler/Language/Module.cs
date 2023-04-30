using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;

namespace Ronin.Language;

internal class Module : Semantics
{
    public static Dictionary<string, Module> All { get; } = new();

    public Context Context { get; init; }
    public List<Instruction> Instructions { get; init; } = new();    

    public Module(Scope scope) { }

    public string GetName(List<Statement> statements)
    {
        string name = string.Empty;
        bool alreadyNamed = false;

        foreach (var statement in statements)
        {
            if (statement is not ImportExport syntax) continue;
            if (syntax.Direction is not PartOf) continue;

            if (alreadyNamed)
            {
                Errors.Add(new ModuleAlreadyNamed { Statement = statement });
                continue;
            }

            foreach (var component in syntax.Components)
            {
                foreach (var token in component.Source.Span)
                {
                    if (token is TextLiteral) name += $" {token.sourcecode[1..^1]}";
                    else name += $" {token.sourcecode}";
                }
            }

            alreadyNamed = true;
        }

        return name is "" ? name : name[1..];
    }

    /*public class Part
    {
        public Part(Scope scope)
        {
            foreach (var statement in scope.Values)
            {
                if (statement.value is not ImportExport syntax) continue;
                if (syntax.Direction is not Import) continue;
                var module = All.GetOrAdd(syntax.Name, _ => new UnresolvedModule());
                Context.Add(module);
            }
        }

        
        
    }*/
}

/*internal class UnresolvedModule : Module
{
    public ConcurrentQueue<Scope> Scopes { get; } = new();

    public UnresolvedModule() { }

    public static Module From(Scope scope)
    {
        UnresolvedModule unresolved = new();
        var name = unresolved.GetName(scope.Values);
        var module = All.GetOrAdd(name, _ => unresolved) as UnresolvedModule;
        module.Scopes.Enqueue(scope);
        if (unresolved != module) module.Errors.AddRange(unresolved.Errors);
        return unresolved;
    }
}*/

internal class ModuleAlreadyNamed : Error { }