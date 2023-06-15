using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Instruction : Semantic
{
    public Result Result { get; init; }
    public List<Result> Inputs { get; init; } = new();

    public Instruction(Syntax syntax) : base(syntax) { }
}

[ExcludeFromCodeCoverage]
internal class FunctionCall : Instruction
{
    public Function Function { get; init; }
    public Context Context { get; init; }

    public FunctionCall(Reference reference, Context context) : base(reference)
    {
        Context = context;
    }
}

[ExcludeFromCodeCoverage]
internal class AssignmentInstruction : Instruction
{
    public UnresolvedDatum Datum { get; }

    public AssignmentInstruction(Assignment assignment, Context context) : base(assignment)
    {
        Datum = new(assignment.Reference, context);
    }
}

[ExcludeFromCodeCoverage]
internal class InitializeDatum : Instruction
{
    public Datum Datum { get; }

    public InitializeDatum(Datum datum) : base(datum.Source)
    {
        Datum = datum;
    }
}


[ExcludeFromCodeCoverage]
internal class InstructionNotAllowedHere : Error { }

[ExcludeFromCodeCoverage]
internal class NotAnInstruction : Error { }