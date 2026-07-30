// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Where one statement ends and the next begins.
/// </summary>
///
/// <remarks>
///     Extracted because two places sequence statements — a braced definition
///     and the top level of a file — and only one of them had the rule. A
///     module merely TRIED to take a terminator and ignored failing, so «1 2;»
///     and «var first = 1 var second = 2» were accepted as two statements with
///     nothing said, while the same tokens inside a block were refused. Moving
///     a statement out of a block changed whether it was legal.
/// </remarks>
internal static class Sequence
{
    /// <summary>
    ///     Whether a statement that took no terminator may still end here.
    /// </summary>
    ///
    /// <remarks>
    ///     A block already says where it stops. Requiring «;» after «if x { … }»
    ///     meant a braced statement could be the LAST thing in a sequence and
    ///     nothing else — «function f { if x { return 1; } return 2; }» did not
    ///     compile, which is most programs.
    /// </remarks>
    public static bool Elides(Parser from, Parser to) => Ended(from, to) is Close.Brace;

    /// <summary>
    ///     The last token an element consumed, so a caller can tell a statement
    ///     that ended with a brace from one that did not.
    /// </summary>
    private static Token Ended(Parser from, Parser to)
    {
        Token last = null;

        for (var token = from.Token; ReferenceEquals(token, to.Token) is false; token = token.Next as Token)
        {
            if (token is not Trivium) last = token;
        }

        return last;
    }
}

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
}
