using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Lookup : Aggregate<Lookup, OpenBrace, Lookup.Association, Separator, CloseBrace>
{
    public class Association : Syntax, Compiler.IParsable<Association>
    {
        public Value Key { get; init; }
        public Value Value { get; init; }

        public static Association Parse(ref Parser context)
        {
            Parser parser = context;

            if (Value.Parse(ref parser) is not Value key) return null;

            if (parser.FailedToConsume<Assign>()) return null;

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
