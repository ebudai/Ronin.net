using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Language;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Compiler;

[ExcludeFromCodeCoverage]
internal ref struct Analyzer
{
    public static List<Error> Analyze(Definition parent, Scope scope)
    {
        scope.Definition.Parent = parent;
        Name name = null;
        List<Error> errors = new();
        foreach (var statement in scope.Definition.Values)
        {
            errors.AddRange(statement switch
            {
                Export export => Analyze(scope, export, ref name),
                Import import => Analyze(scope.Definition, import),
                Function.Declaration declaration => Analyze(scope.Definition, declaration),
                Datatype.Declaration declaration => Analyze(scope.Definition, declaration),
                Datum.Declaration declaration => Analyze(scope.Definition, declaration),
                Scope inner => Analyze(scope.Definition, inner),
                Assignment or Reference or AnonymousValue => Error.None,
                Unknown unknown => Error.UnknownSyntax(unknown),
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

    private static List<Error> Analyze(Definition parent, Definition definition)
    {
        definition.Parent = parent;
        List<Error> errors = new();
        foreach (var statement in definition.Values)
        {
            errors.AddRange(statement switch
            {
                Export export => Error.ExportedScopeMustBeAnonymous(export),
                Import import => Analyze(definition, import),
                Function.Declaration declaration => Analyze(definition, declaration),
                Datatype.Declaration declaration => Analyze(definition, declaration),
                Datum.Declaration declaration => Analyze(definition, declaration),
                Scope inner => Analyze(definition, inner),
                Assignment or Reference or AnonymousValue => Error.None,
                Unknown unknown => Error.UnknownSyntax(unknown),
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

    private static List<Error> Analyze(Definition definition, Import import)
    {
        definition.Imports.Add(new Module.Unresolved { Import = import });
        return Error.None;
    }

    private static List<Error> Analyze(Definition definition, Function.Declaration declaration)
    {
        var errors = Analyze(definition, declaration.Definition);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition
        };

        definition.Add(declaration, function);

        return errors;
    }

    private static List<Error> Analyze(Definition definition, Datatype.Declaration declaration)
    {
        var errors = Analyze(definition, declaration.Definition);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        definition.Add(declaration.Identifier, datatype);

        return errors;
    }

    private static List<Error> Analyze(Definition definition, Datum.Declaration declaration) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        definition.Add(declaration.Name, datum);

        return Error.None;
    }
}
