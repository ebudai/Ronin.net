using Ronin.Grammar;

namespace Ronin.Compiler;

//TODO make this nonstatic
internal static class Analyzer
{
    public static void Define(Definition parent, Definition definition, List<Error> errors)
    {
        definition.Parent = parent;

        foreach (var statement in definition.Statements)
        {
            if (statement is Join export)
            {
                errors.Add(Error.ScopeMustBeAnonymous(definition, export));
                continue;
            }
            Define(definition, statement, errors);
        }
    }

    public static void Define(Definition parent, Scope scope, List<Error> errors)
    {
        scope.Definition.Parent = parent;
        
        Name name = null;
        
        foreach (var statement in scope.Definition.Values)
        {
            if (statement is Join export)
            {
                Export(scope, export, ref name, errors);
                continue;
            }
            Define(scope.Definition, statement, errors);
        }
        
        if (name is not null)
        {
            Global.Scope.Add(name, scope.Definition, errors);
        }
    }

    private static void Define(Definition definition, Statement statement, List<Error> errors)
    {
        switch (statement)
        {
            case Import import: Import(definition, import); break;
            case Function.Declaration function: Define(definition, function, errors); break;
            case Datatype.Declaration datatype: Define(definition, datatype, errors); break;
            case Datum.Declaration datum: Define(definition, datum, errors); break;
            case Scope inner: Define(definition, inner, errors); break;
            case Unknown unknown: errors.Add(Error.UnknownSyntax(unknown)); break;
            default: break;
        }
    }

    private static void Export(Scope scope, Join export, ref Name name, List<Error> errors)
    {
        bool error = false;

        if (scope is not AnonymousScope)
        {
            errors.Add(Error.ScopeMustBeAnonymous(scope.Definition, export));
            error = true;
        }

        if (scope.Modifiers.Source.IsEmpty is false)
        {
            errors.Add(Error.ScopeMustBeUnmodified(scope.Definition, export));
            error = true;
        }

        if (name is not null)
        {
            errors.Add(Error.ScopeIsAlreadyPartOfAModule(scope.Definition, export));
            error = true;
        }
        
        if (error is false) name = export.Name;
    }

    private static void Import(Definition definition, Import import)
    {
        definition.Imports.Add(new Definition.Unresolved { Import = import });
    }

    private static void Define(Definition definition, Function.Declaration declaration, List<Error> errors)
    {
        Define(definition, declaration.Definition, errors);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition
        };

        definition.Add(declaration.Identifier, function, errors);
    }

    private static void Define(Definition definition, Datatype.Declaration declaration, List<Error> errors)
    {
        Define(definition, declaration.Definition, errors);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        definition.Add(declaration.Identifier, datatype, errors);
    }

    private static void Define(Definition definition, Datum.Declaration declaration, List<Error> errors) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        definition.Add(declaration.Name, datum, errors);
    }
}
