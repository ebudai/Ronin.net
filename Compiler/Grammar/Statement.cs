// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
internal class Statement : Syntax, IParsableSyntax<Statement>
{
    public static Statement Parse(scoped ref Parser current)
        => Export.Parse(ref current)
        ?? Import.Parse(ref current)
        ?? FunctionDeclaration.Parse(ref current)
        ?? DatatypeDeclaration.Parse(ref current)
        ?? Assignment.Parse(ref current)
        ?? Reference.Parse(ref current)
        ?? Anonymous.Parse(ref current)        
        ?? DatumDeclaration.Parse(ref current)
        ?? Scope.Parse(ref current)
        ?? Unknown.Parse(ref current) as Statement;
}