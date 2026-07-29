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
    public static Name Parse(ref Parser current) => Parse(ref current, leading: true);

    /// <summary>
    ///     As <see cref="Parse(ref Parser)"/>, but past the point where a keyword
    ///     could steal the declaration.
    /// </summary>
    ///
    /// <remarks>
    ///     The rule is about the FIRST word of an identifier, and it was being
    ///     applied to every name component — so «function send (x) part of (y)»
    ///     stopped at the parameter block and the declaration came back
    ///     Malformed. A keyword in the middle of a phrase announces nothing and
    ///     no outer production can steal anything there, which is exactly why
    ///     «var ready if needed» was always allowed. Glue position is the same
    ///     position, one component along.
    /// </remarks>
    public static Name Continuing(ref Parser current) => Parse(ref current, leading: false);

    private static Name Parse(ref Parser current, bool leading)
    {
        Parser parser = current;

        if (leading && parser.Token is Keyword and not Modifier) return null;

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

    /// <remarks>
    ///     Over the canonical words and not the raw tokens, because that is what
    ///     the name IS. Two names spelled «part of» and «part  of» have the same
    ///     words, the same rendering and the same symbol-table key, and used to
    ///     compare unequal and hash apart — an identity that disagreed with every
    ///     other layer's.
    /// </remarks>
    public override bool Equals(object obj) => (obj as Name)?.Canonical.SequenceEqual(Canonical) ?? false;

    /// <remarks>
    ///     Over the canonical words, to agree with <see cref="Equals"/>. Hashing
    ///     <c>Memory</c> itself compared the backing object, index and length
    ///     rather than the contents, so two names spelling the same thing hashed
    ///     differently unless they happened to share a string instance — which
    ///     interning arranged for often enough that the test asserting hash
    ///     equality passed by accident.
    /// </remarks>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (var word in Canonical)
        {
            hashCode.Add(word);
        }
        return hashCode.ToHashCode();
    }
}
