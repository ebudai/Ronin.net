// Copyright © 2023 Eric Budai

using Ronin.Compiler;
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

    public virtual void ResolveTypes(IContext context) { }

    public virtual void ResolveCalculatedTypes(IContext context, List<Statement> calculations, Stack<Statement> circularityCheck) { }

    public virtual void ResolveFunctions(IContext context) { }

    public virtual void ResolveData(IContext context) { }

    public virtual void ResolveCalculatedData(IContext context) { }
}

internal class Noop : Statement { }

