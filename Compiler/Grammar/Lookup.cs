// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of key=value pairs used to specify associations directly in code.
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-delimited list of <see cref="Association"/>s
/// </remarks>
/// 
/// <example>
///     var a = "one";
///     var b = "the thing";
///     var x = { a = 3, b = 22.3, "special" = values maximum };
///             ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Lookup : Aggregate<Lookup, Open.Brace, Association, Separator, Close.Brace>
{
    [ExcludeFromCodeCoverage]
    public override void ResolveReferences(Scope context)
    {
        foreach (var association in this)
        {
            association.ResolveReferences(context);
        }
    }
}
