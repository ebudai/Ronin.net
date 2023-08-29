// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Hierarchy;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Body of <see cref="Scope"/>s, <see cref="Datatype.Declaration"/>s and <see cref="Function.Declaration"/>s.
/// </summary>
/// 
/// <remarks>
///     <see cref="Terminal"/>-separated <see cref="Statement"/>s between <see cref="StartScope"/> and <see cref="EndScope"/>
/// </remarks>
/// 
/// <example>
///     datatype Speaker
///   → {
///   →     var volume => number; 
///   →     var base => number; 
///   →     var treble => number;
///   →     var brand => text;
///   → }
///     ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Context : Aggregate<Context, StartScope, Statement, Terminal, EndScope>
{
    public Context Parent { get; set; }
    public List<Context> Imports { get; init; } = new();
    public Dictionary<Identifier.Component, Member> Members { get; init; } = new();
    public Dictionary<Identifier.Component, Context> Children { get; } = new();

    public Error Add(Identifier identifier, Member member)
    {
        var existing = Existing(identifier);
        if (existing is not null) return Error.Redefinition(existing, member);

        Context context = this;

        for (int i = 0, max = identifier.Components.Count - 1; i < max; ++i)
        {
            if (context.Children.TryGetValue(identifier.Components[i], out var child) is false)
            {
                child = new Context { Parent = context };
                context.Children.Add(identifier.Components[i], child);
            }
            context = child;
        }

        context.Members.Add(identifier.Components[^1], member);

        return null;
    }

    public virtual Identifier Existing(Identifier identifier)
    {
        List<Identifier.Component> components = new(identifier.Components.Count);
        Context context = this;
        Identifier.Component component;
        for (int i = 0, max = identifier.Components.Count - 1; i < max; ++i)
        {
            (component, context) = context.Children.Entry(identifier.Components[i]);
            if (component is null)
            {
                // failure case
                foreach (var import in Imports)
                {
                    var existing = import.Existing(identifier);
                    if (existing is not null) return existing;
                }
                return Parent?.Existing(identifier);
            }
            components.Add(component);
        }
        component = context.Members.Entry(identifier.Components[^1]).Key;
        components.Add(component);
        return component is null ? null : new Identifier { Components = components };
    }

    public virtual List<Resolution> Resolve(Reference reference)
    {
        throw new NotImplementedException();
    }

    public class Member : Syntax
    {
        public Modifiers Modifiers { get; init; }
    }

    public new class Unresolved : Context
    {
        public required Import Import { get; init; }
    }
}
