using Ronin.Compiler;
using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public Module Global { get; init; } = new();
    //public Stack<>
    public List<IError> Errors { get; } = new();
}
