// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Body of <see cref="Scope"/>s, <see cref="Type.Declaration"/>s and <see cref="Function.Declaration"/>s.
/// </summary>
/// 
/// <remarks>
///     <see cref="Terminal"/>-separated <see cref="Statement"/>s between <see cref="OpenBrace"/> and <see cref="CloseBrace"/>
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
/*internal class Context : Aggregate<Context, OpenBrace, Statement, Terminal, CloseBrace>
{
    public abstract class Member : Value
    {
        public Modifiers Modifiers { get; init; }

        public new static Member Parse(ref Parser current)
        {
            Parser parser = current;

            if (Reference.Parse(ref parser) is not Reference reference) return null;

            return new Unresolved
            {
                Reference = reference,
                Source = parser.Commit(ref current)
            };
        }

        public class Unresolved : Member
        {
            public Reference Reference { get; init; }            
        }

        public class Overloaded : Member
        {
            public List<Resolution> Overloads { get; init; }
        }
    }

    public Context Parent { get; set; }
    public List<Module> Imports { get; } = new();
    public Dictionary<Identifier.Component, Context> Children { get; set; } = new();
    public Dictionary<Identifier.Component, Member> Members { get; set; } = new();

    public void Add(Import import) => Imports.Add(new Module.Unresolved(import));

    public Error Add(Identifier identifier, Member member)
    {
        var context = this;
        for (int i = 0, max = identifier.Components.Count - 1; i < max; ++i)
        {
            if (Children.TryGetValue(identifier.Components[i], out var child) is false)
            {
                child = new() { Parent = context };
                context.Children.Add(identifier.Components[i], child);
            }
            context = child;
        }
        return context.Members.TryAdd(identifier.Components[^1], member) ? null : Error.Redefinition(member);
    }

    public void Define(Context context, List<Error> errors)
    {
        Parent = context;

        foreach (var statement in this)
        {
            switch (statement)
            {
                case Import import: context.Add(import); break;
                case Function.Declaration function: function.Define(this, errors); break;
                case Type.Declaration datatype: datatype.Define(this, errors); break;
                case Datum.Declaration datum: datum.Define(this, errors); break;
                case Delegate.Declaration @delegate: @delegate.Define(this, errors); break;
                case Scope scope: scope.Define(this, errors); break;
                default: Error.UnknownSyntax(this); break;
            }
        }
    }

    public virtual Resolution Resolve(Reference reference) => Resolve(reference.Span);

    private Resolution Resolve(ReadOnlySpan<Reference.Component> reference)
    {
        List<Resolution> resolutions = new();

        Resolve(reference, resolutions);
        
        Parent?.Resolve(reference, resolutions);

        foreach (var module in Imports)
        {
            module.Resolve(reference, resolutions);
        }

        return Resolution.From(resolutions);
    }
        
    private void Resolve(ReadOnlySpan<Reference.Component> reference, List<Resolution> resolutions)
    {
        foreach (var name in reference)
        {
            Value value = name;
            if (value is null) continue;
            
        }

        foreach (var (identifier, child) in Children)
        {
            if (identifier.value is Parameters parameters)
            {
                if (reference.Length is not 0 && reference[0].value is Inputs inputs)
                {

                }                
            }
        }

        foreach (var (name, member) in Members)
        {
            if (name.Equals(reference[0]))
            {
                Resolution.Exact resolution = new() { Member = member };
                Parameters parameters = name;
                if (parameters is not null)
                {
                    //reference.Slice() 
                }
                resolutions.Add(resolution);
            }
        }
    }

    private Resolution[] Resolve(Parameters parameters, ReadOnlySpan<Reference.Component> reference)
    {
        var resolutions = new Resolution[parameters.Data.Count];

        for (int i = 0, max = resolutions.Length; i != max; ++i)
        {
            List<Resolution> inputs = new();
            for (var j = i; j != resolutions.Length; ++j)
            {

            }
            resolutions[i] = Resolution.From(inputs);
        }

        
        
        return resolutions;
    }

    
}*/