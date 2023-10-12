// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
internal abstract class Statement : IParsable<Statement>
{
    public static Statement Parse(ref Parser current)
        => Export.Parse(ref current)
        ?? Import.Parse(ref current)
        ?? Association.Parse(ref current)
        ?? Member.Parse(ref current)        
        ?? Value.Parse(ref current)
        ?? Scope.Parse(ref current)
        ?? Unknown.Parse(ref current) as Statement;

    public virtual void ResolveTypes(Scope context) { }

    public virtual void ResolveCalculatedTypes(Scope context, Stack<Statement> circularityCheck) { }
}

internal class Noop : Statement { }