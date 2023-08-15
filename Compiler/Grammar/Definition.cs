// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
internal class Definition : Aggregate<Definition, StartScope, Statement, Terminal, EndScope>
{
    public List<Statement> Statements => Values;

    public Definition Parent { get; set; }
    public List<Definition> Imports { get; init; } = new();
    public Dictionary<Identifier.Component, Member> Members { get; init; } = new();
    public Dictionary<Identifier.Component, Definition> Children { get; } = new();

    public void Add(Identifier identifier, Member member, List<Error> errors)
    {
        var components = CollectionsMarshal.AsSpan(identifier.Components);

        var existing = Existing(components);
        if (existing.Count is not 0)
        {
            errors.Add(Error.Redefinition(member, new Identifier { Components = existing }));
            return;
        }
        
        Add(components, member, errors);
    }

    public void Add(Name name, Definition definition, List<Error> errors)
    {
        var module = GetModule(name);
        if (module is not null)
        {
            definition.Join(module, errors);
        }
        else
        {
            Identifier identifier = new(name);
            Children.Add(identifier.Components[0], definition);
        }
    }

    public Definition GetModule(Name name)
    {
        Identifier identifier = new(name);
        var components = CollectionsMarshal.AsSpan(identifier.Components);
        return GetModule(components);
    }

    [ExcludeFromCodeCoverage]
    public List<object> Find(Reference reference)
    {
        throw new NotImplementedException();
    }

    public void Join(Definition definition, List<Error> errors)
    {
        foreach (var statement in Statements) definition.Statements.Add(statement);

        foreach (var (identifier, element) in Members)
        {
            definition.Add(new Identifier { Components = new() { identifier } }, element, errors);
        }
        foreach (var (identifier, child) in Children)
        {
            if (definition.Children.TryGetValue(identifier, out var existing))
            {
                child.Join(existing, errors);
                continue;
            }
            definition.Children.Add(identifier, child);
        }
    }

    private Definition GetModule(ReadOnlySpan<Identifier.Component> words)
    {
        if (Children.TryGetValue(words[0], out var module) is false) return null;

        return words.Length is 1 ? module : module.GetModule(words[1..]);
    }

    private List<Identifier.Component> Existing(ReadOnlySpan<Identifier.Component> identifier)
    {
        List<Identifier.Component> components = new();
        Existing(identifier, components);
        return components;
    }

    private void Existing(ReadOnlySpan<Identifier.Component> identifier, List<Identifier.Component> existing)
    {
        Query(identifier, existing);
        if (existing.Count is 0) Parent?.Existing(identifier, existing);
    }

    private void Add(ReadOnlySpan<Identifier.Component> components, Member member, List<Error> errors)
    {
        if (components.Length is 1)
        {
            if (Members.TryAdd(components[0], member) is false)
            {
                Identifier identifier = new();
                identifier.Components.Add(components[0]);
                errors.Add(Error.Redefinition(member, identifier));
            }
            return;
        }

        if (Children.TryGetValue(components[0], out var child) is false)
        {
            child = new Definition { Parent = this };
            Children.Add(components[0], child);
        }

        child.Add(components[1..], member, errors);
    }

    private void Query(ReadOnlySpan<Identifier.Component> identifier, List<Identifier.Component> result)
    {
        if (identifier.Length is 1)
        {
            var name = Members.Entry(identifier[0]).Key;
            if (name is not null) result.Add(name);
            return;
        }
        else
        {
            var (name, child) = Children.Entry(identifier[0]);
            if (child is not null)
            {
                result.Add(name);
                child.Query(identifier[1..], result);
            }
        }        
    }

    public class Member
    {
        public Modifiers Modifiers { get; init; }
    }

    public new class Unresolved : Definition
    {
        public required Import Import { get; init; }
    }
}
