// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Member : Value, IParsable<Member>
{
    public Modifiers Modifiers { get; init; }
    public Identifier Identifier { get; init; }

    public static new Member Parse(ref Parser current)
        => Datum.Parse(ref current)
        ?? Function.Parse(ref current)
        ?? Type.Parse(ref current) as Member;

    public override void ResolveTypes(Scope context) => Identifier.ResolveTypes(context);
    public override void ResolveFunctions(Scope context) => Identifier.ResolveFunctions(context);
    public override void ResolveData(Scope context) => Identifier.ResolveData(context);

    

    public class Overloaded : Member
    {
        public List<Member> Overloads { get; } = new();
    }
}
