using Ronin.Lexicon;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ronin.Compiler;

internal interface IError
{
    Dictionary<string, object> Data { get; }
    string Reason { get; }
    ReadOnlyMemory<Token> Tokens { get; }

    public void IsAbout(object data, [CallerArgumentExpression(nameof(data))] string name = "") => Data.Add(name, data);
}