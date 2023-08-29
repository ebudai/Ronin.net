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

    /*public void Add(Identifier identifier, Member member, List<Error> errors)
    {
        var name = CollectionsMarshal.AsSpan(identifier.Components);

        Identifier existing = new();
        FindExisting(name, existing);
        if (existing.Components.Count == identifier.Components.Count)
        {
            errors.Add(Error.Redefinition(existing, member));
            return;
        }

        Add(name, member);
    }

    public Definition GetModule(Identifier identifier)
    {
        var name = CollectionsMarshal.AsSpan(identifier.Components);
        return GetModule(name);
    }

    public List<Member> Find(Reference reference)
    {
        List<Member> found = new();
        var components = CollectionsMarshal.AsSpan(reference.Components);
        Find(components, found);
        return found;
    }

    public void Join(Definition definition, List<Error> errors)
    {
        foreach (var statement in Statements)
        {
            definition.Statements.Add(statement);
        }

        foreach (var (name, element) in Members)
        {
            Identifier identifier = new();
            identifier.Components.Add(name);
            definition.Add(identifier, element, errors);
        }

        foreach (var (identifier, child) in Children)
        {
            if (definition.Children.TryGetValue(identifier, out var existing))
            {
                child.Join(existing, errors);
            }
            else
            {
                definition.Children.Add(identifier, child);
            }            
        }
    }

    private void Add(ReadOnlySpan<Identifier.Component> components, Member member)
    {
        if (components.Length is 1)
        {
            Members.Add(components[0], member);
            return;
        }

        if (Children.TryGetValue(components[0], out var child) is false)
        {
            child = new Definition { Parent = this };
            Children.Add(components[0], child);
        }

        child.Add(components[1..], member);
    }

    private void FindExisting(ReadOnlySpan<Identifier.Component> identifier, Identifier existing)
    {
        FindExisting(identifier, existing.Components);
        if (existing.Components.Count is 0) Parent?.FindExisting(identifier, existing);
    }    

    private void FindExisting(ReadOnlySpan<Identifier.Component> identifier, List<Identifier.Component> result)
    {
        if (identifier.Length is 1)
        {
            var name = Members.Entry(identifier[0]).Key;
            if (name is not null) result.Add(name);
        }
        else
        {
            var (name, child) = Children.Entry(identifier[0]);
            if (child is not null)
            {
                result.Add(name);
                child.FindExisting(identifier[1..], result);
            }
        }        
    }

    private Definition GetModule(ReadOnlySpan<Identifier.Component> name)
    {
        if (Children.TryGetValue(name[0], out var module) is false)
        {
            module = new();
            Children.Add(name[0], module);
        }

        return name.Length is 1 ? module : module.GetModule(name[1..]);
    }

    private void Find(ReadOnlySpan<Reference.Component> name, List<Member> found)
    {
        if (name.Length is 1)
        {
            var members = GetMembers(name[0]);
            found.AddRange(members);
            return;
        }
        
        foreach (var child in GetChildren(name[0]))
        {
            child.Find(name[1..], found);
        }
    }

    private List<Member> GetMembers(Reference.Component name)
    {
        List<Member> members = new(Members.Count);
        foreach (var entry in Members)
        {
            if (entry.Key.Equals(name)) members.Add(entry.Value);
        }
        return members;
    }

    private List<Definition> GetChildren(Reference.Component name)
    {
        List<Definition> children = new(Children.Count);
        foreach (var entry in Children)
        {
            if (entry.Key.Equals(name)) children.Add(entry.Value);
        }
        return children;
    }*/

    public class Member : Syntax
    {
        public Modifiers Modifiers { get; init; }
    }

    public new class Unresolved : Context
    {
        public required Import Import { get; init; }
    }
}
