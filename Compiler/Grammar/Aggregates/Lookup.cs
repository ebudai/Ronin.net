// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Aggregate of key=value pairs used to specify associations directly in code.
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-delimited list of <see cref="Association"/>s
/// </remarks>
/// 
/// <example>
///     var x = { a = 3, b = 22.3, "special" = values maximum };
///             ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Lookup : Aggregate<Lookup, OpenBrace, Lookup.Association, Separator, CloseBrace>
{
    /// <summary>
    ///     key=value pair
    /// </summary>
    public class Association : Syntax, Compiler.IParsable<Association>
    {
        public Value Key { get; init; }
        public Value Value { get; init; }

        public static Association Parse(ref Parser context)
        {
            Parser parser = context;

            if (Value.Parse(ref parser) is not Value key) return null;

            if (parser.FailsToConsume<Assign>()) return null;

            if (Value.Parse(ref parser) is not Value value) return null;

            return new Association
            {
                Key = key,
                Value = value,
                Source = parser.Commit(ref context),
            };
        }
    }
}
