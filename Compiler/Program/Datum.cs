using Ronin.Compiler;

namespace Ronin.Program;

internal class Datum
{
    internal Mutability Mutability { get; init; }
    internal Datatype Datatype { get; set; }
    internal object Value { get; set; }
}