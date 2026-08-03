using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal interface IError
{
    string Reason { get; }
    ReadOnlyMemory<Token> Tokens { get; }
}

/// <summary>
///     A node that has already been asked whether anything under it failed, and
///     kept the answer.
/// </summary>
///
/// <remarks>
///     The reflective walk is priced for running ONCE over a file. A wrapper
///     that asks it as each node finishes parsing pays it once per node, and
///     each of those walks re-descends everything the walk below it had just
///     covered — so a chain of depth d costs 1 + 2 + … + d. Measured at 45 KB
///     for twelve nested lists and 1.9 MB for ninety-six.
///
///     A node that answered for its own subtree ends the descent there, which
///     makes the property compositional: every walk touches each node once
///     across the whole parse rather than once per ancestor.
/// </remarks>
internal interface IAnswersBroken
{
    /// <summary>Whether anything in this node's subtree, including itself, failed.</summary>
    bool Broken { get; }
}
