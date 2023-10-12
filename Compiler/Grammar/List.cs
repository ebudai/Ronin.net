// Copyright © 2023 Eric Budai

using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     List of <see cref="Value.Temporary"/>s specified directly in code
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-delimited list of <see cref="Value.Temporary"/>s between <see cref="OpenBrace"/> and <see cref="CloseBrace"/>
/// </remarks>
/// 
/// <example>
///     var x = { 1, 2, seven, three };
///             ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class List : Aggregate<List, Open.Brace, Value, Separator, Close.Brace>
{
    [ExcludeFromCodeCoverage]
    public override void ResolveTypes(Scope context)
    {
        foreach (var value in this)
        {
            value.ResolveTypes(context);
        }
    }
}