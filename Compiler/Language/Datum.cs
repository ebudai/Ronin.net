using Ronin.Grammar;
using Ronin.Lexicon.Keywords;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datum : Semantics
{
    public Mutability Mutability { get; init; }
    public Modifiers Is { get; set; }
    public Datatype Datatype { get; init; }
    public Value Initializer { get; init; }

    public static Datum Declare(Grammar.DatumDeclaration datum) => new()
    {
        Mutability = datum.Mutability switch
        {
            Variable => Mutability.Variable,
            Reactive => Mutability.Reactive,
            _ => Mutability.Constant
        },
        Is = datum.Mutability switch
        {
            Compiled => Modifiers.Compiled,
            Shared => Modifiers.Shared,
            Persistent => Modifiers.Persistent,
            _ => 0
        },
        Initializer = datum.Initializer,
        Source = datum,
    };
}

public enum Mutability { Constant, Variable, Reactive }

[Flags] 
public enum Modifiers 
{
    None        = 0,
    Compiled    = 1 << 0, 
    Persistent  = 1 << 1, 
    Shared      = 1 << 2, 
}