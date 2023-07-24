using Ronin.Grammar;
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
                Export export => Export(scope, export, ref name),
                Import import => Import(scope.Definition, import),
                Function.Declaration declaration => Define(scope.Definition, declaration),
                Datatype.Declaration declaration => Define(scope.Definition, declaration),
                Datum.Declaration declaration => Define(scope.Definition, declaration),
                Scope inner => Analyze(scope.Definition, inner),
                Assignment or Reference or AnonymousValue => Error.None,
                Unknown unknown => Error.UnknownSyntax(unknown),
                _ => Error.UnhandledSubclass<Statement>(statement.GetType())
            });
        }
        
        if (name is not null)
        {
            Module module = new() { Definition = scope.Definition };
            errors.AddRange(Module.Main.Add(name, module));
        }

        return errors;
    }

    private static List<Error> Define(Definition parent, Definition definition)
    {
        definition.Parent = parent;
        List<Error> errors = new();
        foreach (var statement in definition.Values)
        {
            errors.AddRange(statement switch
            {
                Export export => Error.CannotBePartOf(definition, export),
                Import import => Import(definition, import),
                Function.Declaration declaration => Define(definition, declaration),
                Datatype.Declaration declaration => Define(definition, declaration),
                Datum.Declaration declaration => Define(definition, declaration),
                Scope inner => Analyze(definition, inner),
                Assignment or Reference or AnonymousValue => Error.None,
                Unknown unknown => Error.UnknownSyntax(unknown),
                _ => Error.UnhandledSubclass<Statement>(statement.GetType())
            });
        }
        return errors;
    }

    private static List<Error> Export(Scope scope, Export export, ref Name name)
    {
        if (scope is not AnonymousScope) return Error.CannotBePartOf(scope.Definition, export);
        if (scope.Modifiers.Source.IsEmpty is false) return Error.CannotBePartOf(scope.Definition, scope.Modifiers);
        if (name is not null) return Error.CannotBePartOf(scope.Definition, export.Name);
        
        name = export.Name;

        return Error.None;
    }

    private static List<Error> Import(Definition definition, Import import)
    {
        definition.Imports.Add(new Module.Unresolved { Import = import });
        return Error.None;
    }

    private static List<Error> Define(Definition definition, Function.Declaration declaration)
    {
        var errors = Define(definition, declaration.Definition);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition
        };

        definition.Add(declaration, function);

        return errors;
    }

    private static List<Error> Define(Definition definition, Datatype.Declaration declaration)
    {
        var errors = Define(definition, declaration.Definition);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        definition.Add(declaration, datatype);

        return errors;
    }

    private static List<Error> Define(Definition definition, Datum.Declaration declaration) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        definition.Add(declaration, datum);

        return Error.None;
    }
}
