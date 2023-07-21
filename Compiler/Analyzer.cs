using Ronin.Grammar;
using Ronin.Language;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Compiler;

[ExcludeFromCodeCoverage]
internal ref struct Analyzer
{
    public static List<Error> Analyze(Scope scope)
    {
        Name name = null;
        List<Error> errors = new();
        foreach (var statement in scope.Definition.Values)
        {
            errors.AddRange(statement switch
            {
                Export export => Analyze(scope, export, ref name),
                Import import => Analyze(scope, import),
                Function.Declaration declaration => Analyze(scope, declaration),
                Datatype.Declaration declaration => Analyze(scope, declaration),
                Datum.Declaration declaration => Analyze(scope, declaration),
                Assignment assignment => Analyze(scope, assignment),
                Reference instruction => Analyze(scope, instruction),
                AnonymousValue value => Analyze(scope, value),
                Scope inner => Analyze(scope, inner),
                Unknown unknown => Error.UnknownSyntax(statement),
                _ => Error.UnhandledSubclass<Statement>(statement)
            });
        }
        
        if (name is not null)
        {
            Module module = new() { Definition = scope.Definition };
            errors.AddRange(Module.Main.Add(name, module));
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

    private static List<Error> Analyze(Scope scope, Import import)
    {
        scope.Definition.Imports.Add(new Module.Unresolved { Import = import });
        return Error.None;
    }

    private static List<Error> Analyze(Scope scope, Function.Declaration declaration)
    {
        var errors = Analyze(scope, declaration.Definition);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition
        };

        scope.Definition.Add(function);

        return errors;
    }

    private static List<Error> Analyze(Scope scope, Datatype.Declaration declaration)
    {
        var errors = Analyze(scope, declaration.Definition);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        scope.Definition.Add(datatype);

        return errors;
    }

    private static List<Error> Analyze(Scope scope, Datum.Declaration declaration) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        return Error.None;
    }

    private static List<Error> Analyze(Scope scope, Assignment assignment)
    {
        return null;
    }

    private static List<Error> Analyze(Scope scope, Reference instruction)
    {
        return null;
    }

    private static List<Error> Analyze(Scope scope, AnonymousValue value)
    {
        return null;
    }

    private static List<Error> Analyze(Scope module, Scope scope)
    {
        scope.Definition.Parent = module.Definition;
        return Analyze(scope);
    }
}
