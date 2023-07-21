using Ronin.Grammar;
using Ronin.Language;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Compiler;

[ExcludeFromCodeCoverage]
internal ref struct Analyzer
{
    public List<Error> Analyze(Scope module)
    {
        Name name = null;
        List<Error> errors = new();
        foreach (var statement in module.Definition.Values)
        {
            errors.AddRange(statement switch
            {
                Export export => Analyze(module, export, ref name),
                Import import => Analyze(module, import),
                Function.Declaration declaration => Analyze(module, declaration),
                Datatype.Declaration declaration => Analyze(module, declaration),
                Datum.Declaration declaration => Analyze(module, declaration),
                Assignment assignment => Analyze(module, assignment),
                Reference instruction => Analyze(module, instruction),
                AnonymousValue value => Analyze(module, value),
                Scope scope => Analyze(module, scope),
                Unknown unknown => Error.UnknownSyntax(statement),
                _ => Error.UnhandledSubclass<Statement>(statement)
            });
        }
        return errors;
    }

    private static List<Error> Analyze(Scope scope, Export export, ref Name name)
    {
        if (scope is not AnonymousScope) return Error.ExportedScopeMustBeAnonymous(export);
        if (scope.Modifiers.Source.IsEmpty is false) return Error.ExportedScopeMustBeUnmodified(scope);
        if (name is not null) return Error.ScopeAlreadyNamed(export);
        
        name = export.Name;

        return Error.None;
    }

    private List<Error> Analyze(Scope scope, Import import)
    {
        return null;
    }

    private List<Error> Analyze(Scope scope, Function.Declaration declaration)
    {
        return null;
    }

    private List<Error> Analyze(Scope scope, Datatype.Declaration declaration)
    {
        return null;
    }

    private List<Error> Analyze(Scope scope, Datum.Declaration declaration) 
    {
        return null;
    }

    private List<Error> Analyze(Scope scope, Assignment assignment)
    {
        return null;
    }

    private List<Error> Analyze(Scope scope, Reference instruction)
    {
        return null;
    }

    private List<Error> Analyze(Scope scope, AnonymousValue value)
    {
        return null;
    }

    private List<Error> Analyze(Scope module, Scope scope)
    {
        scope.Definition.Parent = module.Definition;
        return Analyze(scope);
    }
}
