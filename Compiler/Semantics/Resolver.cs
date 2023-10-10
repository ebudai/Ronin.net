using Ronin.Compiler;
using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Semantics;

internal partial class Resolver
{
    public Module Global { get; init; } = new();
    public List<IError> Errors { get; } = new();
}
