using System.Diagnostics;

namespace Ronin.Parser.Grammar;

[DebuggerDisplay("{Value}")]
internal class Literal : Syntax
{
    internal string Value { get; init; }
    internal string Datatype { get; init; }
}
