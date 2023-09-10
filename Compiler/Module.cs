using Ronin.Compiler;
using Ronin.Grammar;
using System.Collections.Generic;
using static Ronin.Compiler.Resolution;

namespace Ronin;

internal class Module : Context
{
    public List<Context> Contexts { get; } = new();
    public Dictionary<Identifier.Component, Module> Modules { get; } = new();

    public void Add(Context context, Identifier name = null) => GetOrCreate(name).Contexts.Add(context);

    public Module GetOrCreate(Identifier name)
    {
        var module = this;
        for (int i = 0, max = name?.Components.Count ?? 0; i < max; ++i)
        {
            if (module.Modules.TryGetValue(name.Components[i], out var child) is false)
            {
                child = new() { Parent = module };
                module.Modules.Add(name.Components[i], child);
            }
            module = child;
        }
        return module;
    }

    public override Resolution Resolve(Reference reference)
    {
        var resolution = base.Resolve(reference);

        foreach (var context in Contexts)
        {
            var resolved = context.Resolve(reference);
            if (resolved is null) continue;
            if (resolution is not Ambiguous)
            {
                resolution = new Ambiguous { Candidates = new() { resolution } };
            }
            var candidates = (resolution as Ambiguous).Candidates;
            if (resolved is Ambiguous ambiguous)
            {
                candidates.AddRange(ambiguous.Candidates);
            }
            else
            {
                candidates.Add(resolved);
            }
        }

        return resolution;
    }

    public class Unresolved : Module
    {
        public Unresolved(Import import) => Import = import;

        public Import Import { get; }
    }
}