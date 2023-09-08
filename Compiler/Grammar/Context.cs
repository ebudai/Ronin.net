// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Hierarchy;
using Ronin.Lexicon;
using System.Collections.Generic;

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
    public class Member : Value, IParsableSyntax<Member>
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
            public List<Member> Overloads { get; init; }
        }
    }

    public Context Parent { get; set; }
    public List<Module> Imports { get; } = new();
    public Dictionary<Identifier, Member> Members { get; } = new();

    public Error Add(Identifier identifier, Member member)
    {
        if (Members.TryAdd(identifier, member)) return null;
        return Error.Redefinition(Members[identifier]);
    }

    public virtual Resolution Resolve(Reference reference)
    {
        return null;
    }

    public void Add(Import import) => Imports.Add(new Module.Unresolved(import));
}