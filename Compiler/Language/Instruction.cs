using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Instruction : Semantics
{
    public Function Function { get; init; }
    public Result Value { get; init; }
    public List<Instruction> Inputs { get; init; }
    //public Instruction(Syntax syntax) => Source = syntax; //TODO this is not sufficient?

    /*public static List<Instruction> From(Anonymous value) => value switch
    {
        Literal or Grammar.Delegate => new(),
        InlineList list => From(list.Values),
        InlineLookup lookup => From(lookup.Values),
        Arguments arguments => From(arguments.Values),
        _ => throw new DeveloperMistakeUnhandledSubclassException<Anonymous> { Statement = value },
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
    }*/
}

[ExcludeFromCodeCoverage]
internal class UnresolvedInstruction : Instruction
{
    public Reference Reference { get; init; }
}