using System.Diagnostics;

namespace Ronin.Grammar;

[DebuggerDisplay("{Value}")]
public class Literal : Syntax
{
    public string Value { get; init; }
    public string Datatype { get; init; }
}
