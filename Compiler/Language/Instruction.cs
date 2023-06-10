using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Instruction : Semantic
{
    public Function Function { get; init; }
    public Result Result { get; init; }
    public List<Semantic> Inputs { get; init; } = new();
}

[ExcludeFromCodeCoverage]
internal class UnresolvedInstruction : Instruction
{
    public Unresolved UnresolvedFunction { get; init; }

    public new Context Context
    {
        get => base.Context;
        set
        {
            base.Context = value;
            UnresolvedFunction.Context = value;
        }
    }

    public UnresolvedInstruction(Reference reference)
    {
        Source = reference;
        UnresolvedFunction = new Unresolved(reference, reference);
    }
}

[ExcludeFromCodeCoverage]
internal class UnresolvedAssignment : UnresolvedInstruction
{
    public UnresolvedAssignment(Assignment assignment) : base(assignment.Reference)
    {
        UnresolvedFunction = new Unresolved(assignment.Reference, assignment);
    }
}

[ExcludeFromCodeCoverage]
internal class InstructionNotAllowedHere : Error { }

[ExcludeFromCodeCoverage]
internal class NotAnInstruction : Error { }