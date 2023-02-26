using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Semantics
{
    public Identifier Identifier { get; init; }

    public Datatype ReturnDatatype { get; init; }

    public List<Datatype> Datatypes { get; } = new();
    public List<Datum> Data { get; } = new();
    public List<Function> InnerFunctions { get; } = new();
    public List<Module> Modules { get; } = new();

    public List<Instruction> Instructions { get; } = new();

    public Function(Semantics parent) : base(parent) { }

    public Function(FunctionDeclarationSyntax function, Semantics parent) : base(parent)
    {
        Identifier = new(function.Identifier, parent);

        ReturnDatatype = new UnresolvedDatatype(function.Returns, parent);

        foreach (var statement in function.Body.Values)
        {
            switch (statement.value)
            {
                case FunctionDeclarationSyntax:     InnerFunctions.Add(new Function(statement, this));                          break;
                case DatatypeDeclarationSyntax:     Datatypes.Add(new Datatype(statement, this));                               break;
                case DatumDeclarationSyntax:        Data.Add(new Datum(statement, this));                                       break;
                case AssignmentSyntax assignment:   Instructions.Add(new Instruction(assignment, this));                        break;
                case Scope:                         Modules.Add(new Module(statement, this));                                   break;
                case IntervalSyntax:                Instructions.Add(new Noop(statement.value));                                break;
                case Value value:                   Instructions.AddRange(Instruction.From(value, this));                       break;
                case ImportExportSyntax:            Errors.Add(new FunctionCannotJoinNamedScope { Statement = statement });     break;
                case UnknownSyntax:                 Errors.Add(new UnknownSyntaxError { Statement = statement });               break;
                default:                            Errors.Add(new UnknownSyntaxError { Statement = statement });               break;
            }
        }
    }
}

internal class UnresolvedFunction : Function
{
    public UnresolvedFunction(Reference reference, Semantics parent) : base(parent) => Source = reference;
}

[ExcludeFromCodeCoverage]
internal class FunctionCannotJoinNamedScope : Error { }