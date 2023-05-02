using Ronin.Grammar;
using Ronin.Lexicon.Keyword;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datum : Semantics
{
    public Mutability Mutability { get; init; }
    public Modifiers Is { get; set; }
    public Name Name { get; init; }
    public Datatype Datatype { get; init; }
    public Value Initializer { get; init; }

    public Datum(Grammar.Datum datum)
    {
        Mutability = datum.Mutability switch
        {
            Variable => Mutability.Variable,
            Reactive => Mutability.Reactive,
            _ => Mutability.Constant
        };

        Name = datum.Name;

        Initializer = datum.Initializer;
    }
}

[ExcludeFromCodeCoverage]
internal class DatumAlreadyExists : Error { }

public enum Mutability { Constant, Variable, Reactive }
[Flags] public enum Modifiers { Compiled = 1, Optional = 2, Persistent = 4, Shared = 8 }