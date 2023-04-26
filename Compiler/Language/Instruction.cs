using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Instruction : Semantics
{
    public Instruction(Syntax syntax) => Source = syntax;

    public static List<Instruction> From(Value value) => value.value switch
    {
        Literal or Grammar.Delegate => new() { new Noop(value) },
        InlineList list => From(list.Values),
        InlineLookup lookup => From(lookup.Values),
        Arguments arguments => From(arguments.Values),
        Reference reference => new() { new UnresolvedInstruction(reference) },
        //_ => new() { new UnknownSyntaxError { Statement = value } },
    };

    public static List<Instruction> From(List<Value> values)
    {
        List<Instruction> instructions = new();
        foreach (var value in values)
        {
            if (value.value is Reference reference) instructions.Add(new UnresolvedInstruction(reference));
            else instructions.Add(new Noop(value.value));
        }
        return instructions;
    }

    public static List<Instruction> From(List<InlineLookup.Association> associations)
    {
        List<Instruction> instructions = new();
        foreach (var association in associations)
        {
            if (association.Value.value is Reference reference) instructions.Add(new UnresolvedInstruction(reference));
            else instructions.Add(new Noop(association.Value.value));
        }
        return instructions;
    }
}

internal class UnresolvedInstruction : Instruction
{
    public UnresolvedInstruction(Reference reference) : base(reference) { }
}

internal class Noop : Instruction
{
    public Noop(Syntax syntax) : base(syntax) { }
}