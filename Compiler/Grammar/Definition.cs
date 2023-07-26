// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
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
    public List<Module> Imports { get; init; } = new();
    public Dictionary<Identifier.Component, Member> Elements { get; init; } = new();
    public Dictionary<Identifier.Component, Definition> Children { get; } = new();

    public void Add(Identifier identifier, Member element, List<Error> errors)
    {
        var components = CollectionsMarshal.AsSpan(identifier.Components);
        List<Token> existing = new();
        Parent?.Existing(components, existing);
        if (existing.Count is not 0)
        {
            errors.Add(Error.Redefinition(element, existing.AsMemory()));
            return;
        }
        
        Add(components, element, errors);
    }

    public List<object> Find(Reference reference)
    {
        throw new NotImplementedException();
    }

    public void Join(Definition definition, List<Error> errors)
    {
        foreach (var statement in Statements) definition.Statements.Add(statement);

        foreach (var (identifier, element) in Elements)
        {
            definition.Add(new Identifier { Components = new() { identifier } }, element, errors);
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

    protected Module Find(ReadOnlyMemory<Token> words)
    {
        Identifier.Component name = new() { value = new Name { Source = new[] { words.Span[0] } } };

        if (Children.TryGetValue(name, out var module) is false) return null;

        return words.Length is 1 ? module as Module : module.Find(words[1..]);
    }

    private void Existing(ReadOnlySpan<Identifier.Component> identifier, List<Token> existing)
    {
        Query(identifier, existing);
        if (existing.Count is 0) Parent?.Existing(identifier, existing);
    }

    private void Add(ReadOnlySpan<Identifier.Component> components, Member member, List<Error> errors)
    {
        if (components.Length is 1)
        {
            if (Elements.TryAdd(components[0], member) is false) errors.Add(Error.Redefinition(member, components[0].Source));
            return;
        }

        if (Children.TryGetValue(components[0], out var child) is false)
        {
            child = new Definition { Parent = this };
            Children.Add(components[0], child);
        }

        child.Add(components[1..], member, errors);
    }

    private void Query(ReadOnlySpan<Identifier.Component> identifier, List<Token> components)
    {
        if (identifier.Length is 1)
        {
            var name = Elements.Entry(identifier[0]).Key;
            if (name is not null) components.AddRange(name.Source);
        }
        else
        {
            var (name, child) = Children.Entry(identifier[0]);
            if (child is not null)
            {
                components.AddRange(name.Source);
                child.Query(identifier[1..], components);
            }
        }        
    }

    public class Member
    {
        public Modifiers Modifiers { get; init; } = new();
    }
}
