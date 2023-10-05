using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Association : Statement, IGrammar<Association>
{
    public Value Destination { get; init; }
    public Assignment Assignment { get; init; }
    public Value Origin { get; init; }

    public static new Association Parse(ref Parser current)
    {
        Parser parser = current;

        if (Value.Parse(ref parser) is not Value destination) return null;
        if (parser.Token is not Assignment assignment) return null;

        var origin = Value.Parse(ref parser);
        if (origin is IError error) return new Error(error);
        
        current = parser;
        return new Association
        {
            Destination = destination,
            Assignment = assignment,
            Origin = origin
        };
    }

    public class Error : Association, IError
    {        
        public Error(IError error)
        {
            Data = error.Data;
            Reason = error.Reason;
            Tokens = error.Tokens;
        }

        public Dictionary<string, object> Data { get; }
        public string Reason { get; }
        public ReadOnlyMemory<Token> Tokens { get; }
    }
}
