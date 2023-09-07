using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Hierarchy;

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

    public override Resolution Find(Reference reference)
    {
        return base.Find(reference);
    }

    public class Unresolved : Module
    {
        public Unresolved(Import import) => Import = import;

        public Import Import { get; }
    }
}