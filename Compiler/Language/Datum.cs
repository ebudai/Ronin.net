using Ronin.Grammar;
using Ronin.Lexicon.Keyword;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datum : Semantics
{
    public Mutability Mutability { get; init; }
    public Modifiers Is { get; set; }
    public Identifier Name { get; init; }
    public Datatype Datatype { get; init; }
    public Value Initializer { get; init; }

    public static Datum ForwardDeclare(Grammar.Datum datum) => new()
    {
        Mutability = datum.Mutability switch
        {
            Variable => Mutability.Variable,
            Reactive => Mutability.Reactive,
            _ => Mutability.Constant
        },
        Name = datum.Name,
        Initializer = datum.Initializer,
    };
}

[ExcludeFromCodeCoverage]
internal class DatumAlreadyExists : Error { }

public enum Mutability { Constant, Variable, Reactive }

[Flags] 
public enum Modifiers 
{ 
    Compiled    = 1 << 0, 
    Persistent  = 1 << 1, 
    Shared      = 1 << 2, 
}