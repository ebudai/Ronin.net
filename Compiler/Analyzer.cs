using Ronin.Grammar.Compound;
using Ronin.Language;
using System.Diagnostics.CodeAnalysis;

namespace Ronin;

[ExcludeFromCodeCoverage]
internal class Analyzer
{
    public Context Define(Definition scope) => new(scope, Context.Global, canBeNamed: true, instructions: main);

    public void Resolve()
    {

    }

    private readonly List<Instruction> main = new();
}
