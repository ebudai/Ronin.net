using Ronin.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Module
{
    public List<Module> Modules { get; init; } = new();
    public List<Datatype> Datatypes { get; init; } = new();
    public List<Function> Functions { get; init; } = new();
    public List<Datum> Data { get; init; } = new();
    public List<Instruction> Instructions { get; init; } = new();

    public static Module Analyze(ref SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
}
