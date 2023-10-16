using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal interface IError
{
    ReadOnlyMemory<Token> ExtractTokens(params OneOrMoreTokens[] tokens)
    {
        List<Token> extraction = new();
        foreach (var token in tokens)
        {
            if (token.AsToken is not null)
            {
                extraction.Add(token.AsToken);
            }
            else
            {
                extraction.AddRange(token.AsTokens.ToArray());
            }
        }
        return extraction.ToArray();
    }

    string Reason { get; }
    ReadOnlyMemory<Token> Tokens { get; }

    internal class OneOrMoreTokens
    {
        private OneOrMoreTokens(Token token) => AsToken = token;
        private OneOrMoreTokens(ReadOnlyMemory<Token> tokens) => AsTokens = tokens;

        public static implicit operator OneOrMoreTokens(Token token) => new(token);
        public static implicit operator OneOrMoreTokens(ReadOnlyMemory<Token> tokens) => new(tokens);
        
        internal readonly Token AsToken;
        internal readonly ReadOnlyMemory<Token> AsTokens;
    }
}