using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Instruction : Semantics
{
    public Instruction(Syntax syntax) => Source = syntax;

    public static List<Instruction> From(Anonymous value) => value switch
    {
        Literal or Grammar.Delegate => new(),
        InlineList list => From(list.Values),
        InlineLookup lookup => From(lookup.Values),
        Arguments arguments => From(arguments.Values),
        _ => throw new UnhandledSubclassError<Anonymous> { Statement = value },
    };

    public static List<Instruction> From(List<Value> values)
    {
        List<Instruction> instructions = new();
        foreach (var value in values)
        {

            //if (value is Reference reference) instructions.Add(new UnresolvedInstruction(reference));
        }
        return instructions;
    }

    public static List<Instruction> From(List<InlineLookup.Association> associations)
    {
        List<Instruction> instructions = new();
        foreach (var association in associations)
        {
            //if (association.Value is Reference reference) instructions.Add(new UnresolvedInstruction(reference));
        }
        return instructions;
    }
}