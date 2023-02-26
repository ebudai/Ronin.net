using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
internal class Instruction : Semantics
{
    public Instruction(Syntax syntax, Semantics parent) : base(parent) => Source = syntax;

    public static List<Instruction> From(Value value, Semantics parent) => value.value switch
    {
        LiteralSyntax or DelegateSyntax => new() { new Noop(value) },
        InlineListSyntax list => From(list.Values, parent),
        InlineLookupSyntax lookup => From(lookup.Values, parent),
        Arguments arguments => From(arguments.Values, parent),
        Reference reference => new() { new UnresolvedInstruction(reference, parent) },
    };

    public static List<Instruction> From(List<Value> values, Semantics parent)
    {
        List<Instruction> instructions = new();
        foreach (var value in values)
        {
            if (value.value is Reference reference) instructions.Add(new UnresolvedInstruction(reference, parent));
            else instructions.Add(new Noop(value.value));
        }
        return instructions;
    }

    public static List<Instruction> From(List<InlineLookupSyntax.Association> associations, Semantics parent)
    {
        List<Instruction> instructions = new();
        foreach (var association in associations)
        {
            if (association.Value.value is Reference reference) instructions.Add(new UnresolvedInstruction(reference, parent));
            else instructions.Add(new Noop(association.Value.value));
        }
        return instructions;
    }
}

internal class UnresolvedInstruction : Instruction
{
    public UnresolvedInstruction(Reference reference, Semantics parent) : base(reference, parent) { }
}

internal class Noop : Instruction
{
    public Noop(Syntax syntax) : base(syntax, null) { }
}