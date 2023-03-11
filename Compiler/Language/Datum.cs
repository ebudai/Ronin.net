using Ronin.Grammar;
using Ronin.Lexicon;
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

    public Datum(DatumDeclarationSyntax datum)
    {
        Mutability = datum.Mutability switch
        {
            VariableKeyword => Mutability.Variable,
            ReactiveKeyword => Mutability.Reactive,
            _ => Mutability.Constant
        };

        Name = datum.Name;

        Datatype = new UnresolvedDatatype(datum.Datatype);

        Initializer = datum.Initializer;
    }
}

public enum Mutability { Constant, Variable, Reactive }
[Flags] public enum Modifiers { Compiled = 1, Optional = 2, Persistent = 4, Shared = 8 }