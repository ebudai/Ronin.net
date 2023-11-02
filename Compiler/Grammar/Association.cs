// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Association : Statement, IParsable<Association>
{
    public Value Destination { get; set; }
    public Assignment Assignment { get; init; }
    public Value Origin { get; set; }

    public static new Association Parse(ref Parser current)
    {
        Parser parser = current;

        if (Value.Parse(ref parser) is not Value destination) return null;
        if (parser.TryAdvance<Assignment>(out var assignment) is false) return null;

        if (Value.Parse(ref parser) is not Value origin)
        {
            return new ExpectedValueError { Tokens = Unknown.Parse(ref current).Tokens };
        }

        current = parser;
        return new Association
        {
            Destination = destination,
            Assignment = assignment,
            Origin = origin
        };
    }

    public class ExpectedValueError : Association, IError
    {
        public string Reason { get; } = "expected value";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
