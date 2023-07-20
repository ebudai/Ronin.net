// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

/// <summary>
///     Modifies a <see cref="DatatypeDeclaration"/> used to restrict a <see cref="Datum"/> or a <see cref="FunctionDeclaration"/>
/// </summary>
/// 
/// <remarks>Currently limited to <see cref="Compiled"/>, <see cref="Persistent"/>, <see cref="Shared"/>, and <see cref="Optional"/></remarks>
internal class Modifiers : Syntax, IParsableSyntax<Modifiers>
{
    public bool Is<T>() where T : Keyword
    {
        foreach (var token in Source.Span)
        {
            if (token is T) return true;
        }
        return false;
    }

    public static Modifiers Parse(ref Parser current)
    {
        Parser parser = current;
        
        while (parser.IsNotFinished)
        {
            if (parser.Token is not Modifier) break;
            parser.Advance();
        }

        if (current.Token == parser.Token) return null;

        return new Modifiers { Source = parser.Commit(ref current) };
    }
}
