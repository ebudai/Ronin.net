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
internal class UnresolvedAssignment : Instruction
{
    public Unresolved Reference { get; init; }

    public UnresolvedAssignment(Assignment assignment, Context context)
    {
        Context = context;
        Source = assignment;
        Reference = new Unresolved(assignment.Reference, context, assignment);
    }
}

[ExcludeFromCodeCoverage]
internal class UnresolvedInstruction : Instruction
{
    public new Unresolved Function { get; init; }

    public UnresolvedInstruction(Reference reference, Context context)
    {
        Context = context;
        Source = reference;
        Function = new Unresolved(reference, context, reference);
    }
}

[ExcludeFromCodeCoverage]
internal class InstructionNotAllowedHere : Error { }