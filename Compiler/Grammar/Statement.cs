// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
internal class Statement : Syntax, IParsableSyntax<Statement>
{
    public static Statement Parse(ref Parser current)
        => ImportExport.Parse(ref current)
        ?? Function.Parse(ref current)
        ?? Datatype.Parse(ref current)
        ?? Scope.Parse(ref current)
        ?? Assignment.Parse(ref current)
        ?? Reference.Parse(ref current)
        ?? Value.Parse(ref current)        
        ?? Datum.Parse(ref current)
        ?? Unknown.Parse(ref current) as Statement;
}