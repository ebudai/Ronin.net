using Ronin.Grammar.Compound;
using Ronin.Language;
using System.Diagnostics.CodeAnalysis;

namespace Ronin;

[ExcludeFromCodeCoverage]
internal class Analyzer
{
    public static Context Define(Definition scope) => new(scope, Context.Global, true);
}
