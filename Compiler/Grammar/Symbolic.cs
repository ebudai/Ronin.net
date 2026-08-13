// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     A single <see cref="Lexicon.Symbol"/> standing between the <see cref="Name"/>s
///     and values of a <see cref="Reference"/>.
/// </summary>
///
/// <remarks>
///     <para>
///     <c>Name.Parse</c> used to swallow symbols into the name beside them, so
///     «name + things» was one three-token name. It no longer does, and this is
///     where the symbol goes instead: the parser records that one occurred and
///     where, and stops. Binding power, associativity and the shape of the
///     resulting expression belong to <c>Resolver</c>, which scores whole spans
///     and does not want a structure guessed for it beforehand.
///     </para>
///     <para>
///     <see cref="Punctuation"/> is excluded, and <see cref="Bracket"/> with it,
///     since brackets derive from it. Those are the statement and scope
///     boundaries — a reference that consumed a <c>;</c> or a <c>{</c> would run
///     off the end of its own statement.
///     </para>
/// </remarks>
internal class Symbolic : Compiler.IParsable<Symbolic>
{
    public Token Token { get; init; }

    public static Symbolic Parse(ref Parser current)
    {
        // Punctuation is excluded — brackets and «;» are the statement and scope
        // boundaries a reference may not run past. The arrow is the one exception,
        // and only in a TYPE: it punctuates a lookup or a function type there
        // rather than bounding anything, and a name cannot swallow it any more than
        // any other symbol, so it costs nothing. In a value it stays excluded, so
        // «x => { … }» is still a delegate and not a reference with an arrow in it.
        var admissible = current.Token is (Symbol and not Punctuation)
                      || (current.Typing && current.Token is Arrow);

        if (admissible is false) return null;

        var token = current.Token;
        current.Advance();
        return new Symbolic { Token = token };
    }
}
