// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for <see cref="Parameters"/> and <see cref="Inputs"/>
/// </summary>
internal class Name
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    /// <remarks>
    ///     <para>
    ///     A name may not BEGIN with a keyword that announces a production. Every
    ///     <see cref="Keyword"/> is a <see cref="Word"/>, so nothing stopped one
    ///     being swallowed — and because <c>Member.Parse</c> tries a datum before
    ///     a function, and a datum needs no mutability once it has a type,
    ///     «function f =&gt; Number { return 1; }» satisfied the datum production
    ///     first as a two-word name «function f» of type «Number». It won. So did
    ///     «if ready =&gt; result», «when changing ready =&gt; result» and
    ///     «iterate banks =&gt; bank», each becoming a declaration of something
    ///     named after the keyword that introduced it, with no findings.
    ///     </para>
    ///     <para>
    ///     Whether the AST came out right depended on what followed the arrow:
    ///     «if ready =&gt; 1» stayed a conditional because a number cannot be
    ///     mistaken for a type.
    ///     </para>
    ///     <para>
    ///     The FIRST word only, because that is where the theft happens — a
    ///     keyword in the middle of a phrase announces nothing, and «var ready if
    ///     needed» is a name the language allows. Modifiers are excluded even
    ///     there: «var hidden cost» is a name too.
    ///     </para>
    ///     <para>
    ///     No exception for «in», which is now reserved nowhere at all. It was a
    ///     keyword briefly, which reserved it in the lexer — unconditionally,
    ///     untypeably, and unscopeably — for a rule that turns out to have no
    ///     safety content: a SINGLE-word «in» cannot capture anything, because
    ///     capture needs a multi-word name straddling a hole. And the multi-word
    ///     case went the same way once the loop's declaring hole was pinned: a
    ///     hole fixed to one token cannot grow across the word after it, so the
    ///     split point is determined by the pattern's shape rather than by taking
    ///     a word away from names.
    ///     </para>
    /// </remarks>
    public static Name Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is Keyword and not Modifier) return null;

        while (parser.Token is Word)
        {
            parser.Advance();
        }

        if (ReferenceEquals(parser.Token, current.Token)) return null;

        return new Name { Tokens = current.AdvanceTo(parser) };
    }

    /// <summary>
    ///     The name as a sequence of word identities, which is what it IS.
    /// </summary>
    ///
    /// <remarks>
    ///     First-class, and every consumer takes it rather than taking
    ///     <see cref="Words"/> apart again. Rendering to a string and splitting
    ///     on spaces was how a pattern segment list got built, and a multi-word
    ///     keyword does not survive that trip: «compute part of (_)» became four
    ///     segments where the lexer emits three lexemes, so the pattern was
    ///     declared, printed correctly, and could never match anything. Doubled
    ///     spacing added an EMPTY segment on top.
    /// </remarks>
    public string[] Canonical => [.. Tokens.ToArray().Select(token => token.Canonical)];

    /// <summary>The name as a symbol table holds it: its words, space separated.</summary>
    public string Words => string.Join(' ', Canonical);

    public override bool Equals(object obj) => (obj as Name)?.Tokens.Span.SequenceEqual(Tokens.Span) ?? false;

    /// <remarks>
    ///     Over each token's characters. Hashing <c>Memory</c> itself compared the
    ///     backing object, index and length rather than the contents, so two names
    ///     spelling the same thing hashed differently unless they happened to share
    ///     a string instance — which interning arranged for often enough that the
    ///     test asserting hash equality passed by accident.
    /// </remarks>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (var token in Tokens.Span)
        {
            hashCode.Add(token.GetHashCode());
        }
        return hashCode.ToHashCode();
    }
}
