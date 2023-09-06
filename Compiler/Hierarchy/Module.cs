using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Hierarchy;

internal class Module : Context
{
    public static readonly Module Global = new();

    private readonly Dictionary<Identifier.Component, Module> Modules = new();
    private readonly List<Context> Contexts = new();

    public void Add(Context context, Identifier name = null)
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
        module.Contexts.Add(context);
    }

    public override Resolution Find(Reference reference)
    {
        return base.Find(reference);
    }

    public new class Unresolved : Module
    {
        public Unresolved(Import import) => Import = import;

        public new Import Import { get; }
    }
}