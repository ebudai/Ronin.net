using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ronin.Grammar;

internal class Module : Scope
{
    private readonly ConcurrentDictionary<Token, Module> children = new();
    private readonly ConcurrentBag<Scope> scopes = new();

    public static new Module Parse(ref Parser current)
    {
        Parser parser = current;

        Module values = new();

        while (Statement.Parse(ref parser) is Statement statement)
        {
            values.Add(statement);
            parser.TryAdvance<Terminal>();
        }

        current = parser;
        return values;
    }

    public override void ResolveTypes(Scope context)
    {
        Parent = context;
        base.ResolveTypes(this);
        foreach (var scope in scopes)
        {
            scope.ResolveTypes(this);
        }
        foreach (var child in children.Values)
        {
            child.ResolveTypes(this);
        }
    }

    public Module GetOrAdd(Name name)
    {
        var module = this;

        foreach (var token in name.Tokens.Span)
        {
            module = module.children.GetOrAdd(token, static _ => new Module());
        }

        return module;
    }

    public Module this[Name name]
    {
        get
        {
            var module = this;

            foreach (var token in name.Tokens.Span)
            {
                if (module.children.TryGetValue(token, out module) is false) return null;
            }

            return module;
        }
    }

    public void Add(Scope scope) => scopes.Add(scope);

    public override Member Find(Reference reference)
    {
        throw new System.NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public class Unresolved : Module 
    {
        public Name Name { get; init; }
    }
}
