using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Module : IContext
{
    public IContext Parent { get; set; }
    public List<Scope> Scopes { get; } = new();
    public Dictionary<Token, Module> Modules { get; } = new();

    public Module() { }
    public Module(Scope scope) => Scopes.Add(scope);

    public static Module Parse(ref Parser current)
    {
        Parser parser = current;

        Scope scope = new();

        while (Statement.Parse(ref parser) is Statement statement)
        {
            scope.Statements.Add(statement);
            parser.TryAdvance<Terminal>();
        }

        current = parser;
        return new Module(scope);
    }

    public Module GetOrAdd(Name name)
    {
        var module = this;

        foreach (var token in name.Tokens.Span)
        {
            if (module.Modules.TryGetValue(token, out module) is false)
            {
                module = new();
                module.Modules.Add(token, module);
            }
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
                if (module.Modules.TryGetValue(token, out module) is false)
                {
                    module = Parent as Module;
                    return module?[name];
                }
            }

            return module;
        }
    }

    public void Add(Scope scope) => Scopes.Add(scope);

    public Resolution Resolve(Reference reference)
    {
        List<Resolution> resolutions = new();
        foreach (var scope in Scopes)
        {
            if (scope.Resolve(reference) is Resolution resolution)
            {
                resolutions.Add(resolution);
            }
        }
        return Resolution.From(resolutions);
    }

    public class Unresolved : Module
    {
        public Name Name { get; init; }
    }
}
