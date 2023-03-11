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

    public Function() { }

    public Function(FunctionDeclarationSyntax function)
    {
        Identifier = new(function.Identifier);

        ReturnDatatype = new UnresolvedDatatype(function.Returns);

        foreach (var statement in function.Body.Values)
        {
            switch (statement.value)
            {
                case FunctionDeclarationSyntax syntax:      InnerFunctions.Add(new Function(syntax));                                   break;
                case DatatypeDeclarationSyntax datatype:    Datatypes.Add(new Datatype(datatype));                                      break;
                case DatumDeclarationSyntax datum:          Data.Add(new Datum(datum));                                                 break;
                case AssignmentSyntax assignment:           Instructions.Add(new Instruction(assignment));                              break;
                case Scope scope:                           Modules.Add(UnresolvedModule.From(scope));                                  break;
                case IntervalSyntax:                        Instructions.Add(new Noop(statement.value));                                break;
                case Value value:                           Instructions.AddRange(Instruction.From(value));                             break;
                case ImportExportSyntax:                    Errors.Add(new FunctionCannotJoinNamedScope { Statement = statement });     break;
                case UnknownSyntax:                         Errors.Add(new UnknownSyntaxError { Statement = statement });               break;
                default:                                    Errors.Add(new UnknownSyntaxError { Statement = statement });               break;
            }
        }
    }
}

internal class UnresolvedFunction : Function
{
    public UnresolvedFunction(Reference reference) => Source = reference;
}

[ExcludeFromCodeCoverage]
internal class FunctionCannotJoinNamedScope : Error { }