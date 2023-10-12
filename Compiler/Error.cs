using OneOf;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal interface IError
{
    ReadOnlyMemory<Token> ExtractTokens(params OneOf<Token, ReadOnlyMemory<Token>>[] tokens)
    {
        List<Token> extraction = new();
        foreach (var token in tokens)
        {
            token.Switch(/* single token */ extraction.Add, /* multiple tokens */ AddRange);
        }
        return extraction.ToArray();

        void AddRange(ReadOnlyMemory<Token> tokens) => extraction.AddRange(tokens.ToArray());
    }

    string Reason { get; }
    ReadOnlyMemory<Token> Tokens { get; }
}