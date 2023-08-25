// Copyright © 2023 Eric Budai

using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Datum.Declaration"/> used to declare variables to enter into a <see cref="Function.Declaration"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Datum"/>s between <see cref="StartValues"/> and <see cref="EndValues"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : Aggregate<Parameters, StartValues, Datum.Declaration, Separator, EndValues>
{
    public Dictionary<Identifier, Datum> Data { get; } = new();

    public override bool Equals(object obj) => (obj as Parameters)?.Values.SequenceEqual(Values) ?? false;

    public override int GetHashCode() => Values.ToHashCode();
}