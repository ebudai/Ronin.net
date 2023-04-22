using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using System.Collections.Concurrent;

namespace Ronin.Language;

internal class Module : Semantics
{
    public List<Part> Parts { get; } = new();

    public List<Instruction> Instructions => Parts.SelectMany(static part => part.Instructions).ToList();

    public static ConcurrentDictionary<string, Module> All { get; } = new();

    public Module() { }

    protected internal string GetName(List<Statement> statements)
    {
        string name = string.Empty;
        bool named = false;

        foreach (var statement in statements)
        {
            if (statement.value is not ImportExport syntax) continue;
            if (syntax.Direction is not PartOf) continue;

            if (named)
            {
                Errors.Add(new ModuleAlreadyNamed());
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

            named = true;
        }

        return name is "" ? name : name[1..];
    }

    public class Part
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

        public Context Context { get; init; }
        public List<Instruction> Instructions { get; init; } = new();
    }
}

internal class UnresolvedModule : Module
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
}

internal class ModuleAlreadyNamed : Error { }