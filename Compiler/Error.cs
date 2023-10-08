using Ronin.Lexicon;
using System;

namespace Ronin.Compiler;

internal interface IError
{
    string Reason { get; }
    ReadOnlyMemory<Token> Tokens { get; }
}