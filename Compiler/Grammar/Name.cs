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
    ///     Anywhere in the name, not just at the front, because «in» has to split
    ///     a loop header: «for each bank in banks» is one name, one keyword and
    ///     one expression, and a name that swallowed the «in» would leave the
    ///     loop unparseable. Modifiers are the exception — «var hidden cost» is a
    ///     name the language already accepts.
    ///     </para>
    /// </remarks>
    public static Name Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.Token is Word && parser.Token is not Keyword or Modifier)
        {
            parser.Advance();
        }

        if (ReferenceEquals(parser.Token, current.Token)) return null;

        return new Name { Tokens = current.AdvanceTo(parser) };
    }

    /// <summary>Where it was written, given the text the tokens came from.</summary>
    public Span Span(SourceText source)
    {
        var first = Tokens.Span[0];
        var last = Tokens.Span[^1];

        return source.Span(first.Offset, last.Offset - first.Offset + last.Memory.Length);
    }

    /// <summary>The name as a symbol table holds it: its words, space separated.</summary>
    public string Words => string.Join(' ', Tokens.ToArray().Select(token => token.Memory.ToString()));

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
