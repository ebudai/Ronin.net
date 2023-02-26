using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Module : Semantics
{
    public List<ImportExportSyntax.Component> Name { get; init; } = new();
    
    public List<Module> Modules { get; init; } = new();
    public List<Datatype> Datatypes { get; init; } = new();
    public List<Function> Functions { get; init; } = new();
    public List<Datum> Data { get; init; } = new();
    public List<Instruction> Instructions { get; init; } = new();

    public Module Global
    {
        get
        {
            if (Parent is null) return this;
            var global = Parent as Module;
            while (global.Parent is not null) global = global.Parent as Module;
            return global;
        }
    }

    public Module(Scope scope, Semantics parent) : base(parent)
    {
        foreach (var statement in scope.Values)
        {
            switch (statement.value)
            {
                case ImportExportSyntax hierarchy: Name = hierarchy.Components; break;
                case AssignmentSyntax assignment: Instructions.Add(new Instruction(assignment, this)); break;
                case FunctionDeclarationSyntax function: Functions.Add(new Function(function, this)); break;
                case DatatypeDeclarationSyntax datatype: Datatypes.Add(new Datatype(datatype, this)); break;
                case Scope inner:
                    {
                        Module module = new(inner, parent);
                        Modules.Add(module);
                        break;
                    }
                case IntervalSyntax: Instructions.Add(new Noop(statement.value)); break;
                case DatumDeclarationSyntax datum: Data.Add(new Datum(datum, parent)); break;
                default: Errors.Add(new UnknownSyntaxError { Statement = statement }); break;
            }
        }
    }

    public void Resolve()
    {
        var global = Global;

    }
}
