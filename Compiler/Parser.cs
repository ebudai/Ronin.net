// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : IParsable<T>
{
    static abstract T Parse(ref Parser current);
}

internal struct Parser
{
    public Parser(Token start) => Token = start;

    public Token Token;

    /// <summary>
    ///     Whether a brace here opens a scope's body rather than continuing the
    ///     expression before it.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     An anonymous value after a word is an argument — «thing 7 ("stuff")»
    ///     is one call — and a brace opens one, so a heading read «if c { 1 }» as
    ///     the reference «c» applied to the list «{ 1 }» and then found no body.
    ///     Every conditional and every loop whose body was a single bare
    ///     expression was malformed for that reason: «if c { 1; }» compiled and
    ///     «if c { 1 }» did not, because a «;» is what stopped «{ 1 }» being a
    ///     list.
    ///     </para>
    ///     <para>
    ///     So the brace is where the heading ends. The bill was a braced
    ///     ARGUMENT in heading position and nothing else, and it is not being
    ///     paid today: a list is bracketed now, so «if takes [ 1 ] { 2 }» is one
    ///     conditional with an argument and nothing a heading could absorb
    ///     begins with a brace.
    ///     </para>
    ///     <para>
    ///     A field of the parser and not a parameter threaded through: the parser
    ///     is a struct copied at every speculative parse, so this reaches the
    ///     component that has to honour it and unwinds with the copy that failed.
    ///     </para>
    ///     <para>
    ///     DORMANT since lists and lookups moved to brackets: nothing in
    ///     <c>Temporary.Parse</c> opens on a brace any more, so no heading can
    ///     absorb one and removing this check fails no test. It is kept because
    ///     the next planned change puts a brace-opening value back — a block that
    ///     is an expression — and the ambiguity returns with it, at which point
    ///     this is what stops «if c { a }» being a call again.
    ///     </para>
    /// </remarks>
    public bool Heading;

    /// <summary>
    ///     Whether a reference is being read as a TYPE annotation rather than a
    ///     value expression.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A field of the parser and not a widening of the reference grammar, for
    ///     the reason the last ruling asked to check: a type annotation's span is
    ///     already delimited by the «;» or the initialiser «=», so admitting the
    ///     arrow INSIDE it does not admit one inside a value expression. The lexer
    ///     is unchanged — it produces <c>Arrow</c> everywhere — and only the span
    ///     capture reads this, which is context the parser already keeps.
    ///     </para>
    ///     <para>
    ///     Two things turn on it, both local to a type. The arrow «=&gt;» becomes an
    ///     ordinary reference symbol, so «lookup text =&gt; number» and the function
    ///     type «text =&gt; number» are one span rather than stopping at the arrow.
    ///     And a name followed by an arrow is NOT a delegate's parameter — there are
    ///     no delegates in a type — so it is taken as an ordinary component and the
    ///     arrow left to stand on its own.
    ///     </para>
    ///     <para>
    ///     Set the same way <see cref="Heading"/> is, and copied with the struct,
    ///     so it reaches the component that honours it and unwinds with a
    ///     speculative parse that failed.
    ///     </para>
    /// </remarks>
    public bool Typing;

    public readonly bool IsNotFinished => Token is not Sentinel;

    /// <summary>
    ///     Whether the parser sits where a statement can be resumed from, which
    ///     is as far as recovery ever scans.
    /// </summary>
    public readonly bool IsAtBoundary => Token is Sentinel or Terminal or Separator or Close;

    /// <summary>
    ///     How deep the grammar may nest before a file is refused rather than
    ///     parsed. Far past anything written on purpose, far short of what the
    ///     stack holds.
    /// </summary>
    public const int MaxNesting = 256;

    /// <summary>
    ///     How many nested groups one file may parse in total, however they are
    ///     arranged.
    /// </summary>
    ///
    /// <remarks>
    ///     A defensive ceiling on total group attempts, and no current
    ///     production is known to be super-linear.
    ///     <para>
    ///     It exists because that has been false twice. Three productions opened
    ///     on «{» — a lookup, a list, and an association through a lookup's value
    ///     — each re-parsing a nested body before it could tell whether it
    ///     matched, so twelve levels took ten seconds. Moving lists and lookups
    ///     to «[» carried the same curve across, and folding them into one
    ///     production removed it. Depth alone bounds neither: the shape of the
    ///     nesting is what costs, so the TOTAL is what is capped.
    ///     </para>
    ///     <para>
    ///     A ceiling whose comment names a fixed bug reads as dead code and gets
    ///     deleted. This one names its own history instead, because the failure
    ///     mode is a hang rather than a wrong answer.
    ///     </para>
    /// </remarks>
    public const int MaxGroups = 1_000_000;

    /// <summary>
    ///     Enters one level of nesting, or refuses.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Nesting is the grammar's only unbounded recursion, and every kind of it
    ///     — braces, brackets, parameter blocks, lists — comes through
    ///     <see cref="Aggregate{TParent, TOpen, TElement, TSeparator, TClose}"/>,
    ///     which is why one guard here covers all of it. A file of fifty thousand
    ///     open braces ended the process with a StackOverflowException, which
    ///     cannot be caught, so nothing downstream could have reported it: it took
    ///     the audit's own test host with it.
    ///     </para>
    ///     <para>
    ///     Thread static rather than a field on this struct, because a parser is
    ///     copied at every production and assigned back only on success — a depth
    ///     carried in the copy would be restored by backtracking on some paths and
    ///     inherited by the caller on others.
    ///     </para>
    /// </remarks>
    public static bool Nest()
    {
        if (nesting >= MaxNesting || ++groups > MaxGroups) return false;

        ++nesting;
        return true;
    }

    public static void Unnest() => --nesting;

    /// <summary>Starts a fresh budget. One file's work is its own.</summary>
    private static void Budget()
    {
        nesting = 0;
        groups = 0;
    }

    [ThreadStatic]
    private static int nesting;

    [ThreadStatic]
    private static int groups;

    /// <summary>
    ///     The tokens an error node carries — everything the caller had not
    ///     consumed, through to the end of the statement — with the caller left
    ///     past all of it.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     One place for what every error production had been doing slightly
    ///     differently. Three spellings had grown up: advance the caller to where
    ///     the local parser stopped, rescan from the caller with
    ///     <see cref="Unknown"/>, or — in one case — scan a local copy and never
    ///     assign the caller at all.
    ///     </para>
    ///     <para>
    ///     That last one is why this exists rather than a convention. A
    ///     production returning a node without consuming a token leaves its
    ///     caller exactly where it was, so <c>Module.Parse</c> reparses the same
    ///     token forever and appends a fresh error statement every time round:
    ///     «var +;» was not a hang but an out-of-memory, and a one-line malformed
    ///     file could take a machine down. Recovering and advancing the caller
    ///     are the same act here, which is what makes the other thing
    ///     unspellable.
    ///     </para>
    /// </remarks>
    public static ReadOnlyMemory<Token> Recover(ref Parser current, Parser stopped)
    {
        var depth = 0;

        // To the end of the statement, so what failed is reported once and the
        // next parse starts at a boundary rather than inside the wreckage — and
        // a group crossed on the way is taken whole. Stopping at the first
        // closer meant «function f => {}» consumed its «{» and left the «}»
        // behind, so one mistake produced a missing type AND unexpected input,
        // which is precisely what the message promises it will not do.
        while (stopped.Token is not Sentinel && (depth is not 0 || stopped.IsAtBoundary is false))
        {
            if (stopped.Token is Open) ++depth;
            else if (stopped.Token is Close) --depth;

            stopped.Advance();
        }

        return current.AdvanceTo(stopped);
    }

    public Module Parse()
    {
        Budget();

        return Module.Parse(ref this);
    }

    public List<T> ParseRepeating<T>() where T : IParsable<T>
    {
        List<T> parsed = [];
        while (IsNotFinished)
        {
            var syntax = T.Parse(ref this);
            if (syntax is null) break;
            parsed.Add(syntax);
        }
        return parsed;
    }

    public void Advance()
    {
        do Token = Token.Next as Token; while (Token is Trivium);
    }

    public bool TryAdvance<T>() where T : Token
    {
        var advance = Token is T;
        if (advance)
        {
            Advance();
        }
        return advance;
    }

    public bool TryAdvance<T>(out T token) where T : Token
    {
        token = Token as T;
        if (token is not null)
        {
            Advance();
            return true;
        }
        return false;
    }

    public bool TryAdvanceMany<T>() where T : Token
    {
        bool advanced = false;
        while (TryAdvance<T>()) advanced = true;
        return advanced;
    }

    /// <remarks>
    ///     Sized by what is collected rather than by the running index, which
    ///     counts trivia that <see cref="Advance"/> then skips — so a name built
    ///     from a token list containing whitespace used to come back padded with
    ///     nulls.
    /// </remarks>
    public ReadOnlyMemory<Token> AdvanceTo(Parser parser)
    {
        List<Token> tokens = [];

        while (ReferenceEquals(Token, parser.Token) is false)
        {
            tokens.Add(Token);
            Advance();
        }

        return tokens.ToArray();
    }
}
