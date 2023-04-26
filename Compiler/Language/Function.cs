using Ronin.Grammar;
using Ronin.Grammar.Compound;
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

    public Function(Grammar.Function function)
    {
        Identifier = function.Identifier;

        ReturnDatatype = new UnresolvedDatatype(function.Returns);

        foreach (var statement in function.Body.Values)
        {
            switch (statement.value)
            {
                case Grammar.Function syntax: InnerFunctions.Add(new Function(syntax));                                   break;
                case Grammar.Datatype datatype: Datatypes.Add(new Datatype(datatype));                                      break;
                case Grammar.Datum datum: Data.Add(new Datum(datum));                                                 break;
                case Assignment assignment: Instructions.Add(new Instruction(assignment));                              break;
                case Scope scope: Modules.Add(UnresolvedModule.From(scope));                                  break;
                case Interval: Instructions.Add(new Noop(statement.value));                                break;
                case Value value: Instructions.AddRange(Instruction.From(value));                             break;
                case ImportExport: Errors.Add(new FunctionCannotJoinNamedScope { Statement = statement });     break;
                case Unknown: Errors.Add(new UnknownSyntaxError { Statement = statement });               break;
                default: Errors.Add(new UnknownSyntaxError { Statement = statement });               break;
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