using Ronin.Grammar;
using Ronin.Lexicon.Keywords;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datum : Semantic
{
    public Mutability Mutability { get; }
    public Modifiers Is { get; }
    public Datatype Datatype { get; }
    public Value Initializer { get; }

    public Datum(DatumDeclaration datum, Context context)// : base(datum)
    {
        Mutability = datum.Mutability switch
        {
            Variable => Mutability.Variable,
            Reactive => Mutability.Reactive,
            _ => Mutability.Constant
        };

        Is = datum.Mutability switch
        {
            Compiled => Modifiers.Compiled,
            Shared => Modifiers.Shared,
            Persistent => Modifiers.Persistent,
            _ => Modifiers.None,
        };
    }
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