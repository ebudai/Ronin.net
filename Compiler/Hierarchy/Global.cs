using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Hierarchy;

internal class Global : Module
{
    public static readonly Global Module = new();

    public Module GetOrAddModule(Identifier identifier)
    {
        if (Modules.TryGetValue(identifier, out var module) is false)
        {
            Identifier parent = new() { Components = new(identifier.Components) };
            parent.Components.RemoveAt(parent.Components.Count - 1);
            module = parent.Components.Count is 0 ? Module : new() { Parent = GetOrAddModule(parent) };
            Modules.Add(identifier, module);
        }
        
        return module;
    }

    private readonly Dictionary<Identifier, Module> Modules = new();
}
