using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datatype : Semantics
{
    public bool IsOptional { get; init; }

    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public Datatype() { }

    public static Datatype Declare(Grammar.Datatype grammar, Semantics parent)
    {
        Datatype datatype = new() { Source = grammar, Parent = parent };
        //datatype.

        return datatype;
    }
}