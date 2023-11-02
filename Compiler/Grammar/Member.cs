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
}
