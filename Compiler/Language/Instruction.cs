using Ronin.Grammar;

namespace Ronin.Language;

internal class Instruction : Semantic
{
    public Result Result { get; init; }
    public List<Result> Inputs { get; init; } = new();

    public Instruction(Syntax syntax) : base(syntax) { }
}

internal class FunctionCall : Instruction
{
    public Function Function { get; init; }
    public Context Context { get; init; }

    public FunctionCall(Reference reference, Context context) : base(reference)
    {
        Context = context;
    }
}

internal class SetValue : Instruction
{
    public UnresolvedDatum Datum { get; }

    public SetValue(Assignment assignment, Context context) : base(assignment)
    {
        Datum = new(assignment.Reference, context);
    }
}

internal class InitializeDatum : Instruction
{
    public Datum Datum { get; }

    public InitializeDatum(Datum datum) : base(datum.Source)
    {
        Datum = datum;
    }
}

internal partial class Errors
{
    public static List<Error> InstructionNotAllowedHere(Statement statement) => new() { new InstructionNotAllowedHere { Statement = statement } };
    public static List<Error> NotAnInstruction(Statement statement) => new() { new NotAnInstruction { Statement = statement } };
}

internal class InstructionNotAllowedHere : Error { }
internal class NotAnInstruction : Error { }