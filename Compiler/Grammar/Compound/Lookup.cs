// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Compound;

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
internal class Lookup : Aggregate<Lookup, StartScope, Lookup.Association, Separator, EndScope>
{
    /// <summary>
    ///     key=value pair
    /// </summary>
    public class Association : Syntax, IParsableSyntax<Association>
    {
        public Value Key { get; init; }
        public Value Value { get; init; }

        public static Association Parse(ref Parser current)
        {
            Parser parser = current;

            if (Value.Parse(ref parser) is not Value key) return null;

            if (parser.TryAdvance<Assign>() is false) return null;

            if (Value.Parse(ref parser) is not Value value) return null;

            return new Association
            {
                Key = key,
                Value = value,
                Source = parser.Commit(ref current),
            };
        }
    }
}
