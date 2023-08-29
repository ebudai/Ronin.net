using Ronin.Grammar;

namespace Ronin.Hierarchy;

internal class Global : Context
{
    private Global() : base() { }

    public static readonly Global Scope = new();

    public void Add(Identifier identifier, Context module)
    {
        Context child;
        Context context = this;

        for (int i = 0, max = identifier.Components.Count - 1; i < max; ++i)
        {
            if (context.Children.TryGetValue(identifier.Components[i], out child) is false)
            {
                child = new() { Parent = context };
                context.Children.Add(identifier.Components[i], child);
            }
            context = child;
        }

        var name = identifier.Components[^1];
        if (context.Children.TryGetValue(name, out child))
        {
            if (child is Module existing)
            {
                existing.Contexts.Add(module);
                return;
            }
            module = new Module { Contexts = new() { module, child } };
        }

        context.Children[name] = module;
    }

    public Module Get(Identifier identfier)
    {
        Context context = this;
        foreach (var name in identfier.Components) 
        {
            if (context.Children.TryGetValue(name, out context) is false) return null;
        }
        return context as Module;
    }
}
