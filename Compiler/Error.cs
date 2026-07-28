using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal interface IError
{
    string Reason { get; }
    ReadOnlyMemory<Token> Tokens { get; }
}
