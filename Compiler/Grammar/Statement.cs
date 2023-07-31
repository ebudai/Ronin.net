// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
internal class Statement : Syntax, IParsableSyntax<Statement>
{
    public static Statement Parse(ref Parser current)
        => Join.Parse(ref current)
        ?? Import.Parse(ref current)
        ?? Function.Declaration.Parse(ref current)
        ?? Datatype.Declaration.Parse(ref current)
        ?? Assignment.Parse(ref current)
        ?? Reference.Parse(ref current)
        ?? AnonymousValue.Parse(ref current)        
        ?? Datum.Declaration.Parse(ref current)
        ?? Scope.Parse(ref current)
        ?? Unknown.Parse(ref current) as Statement;
}