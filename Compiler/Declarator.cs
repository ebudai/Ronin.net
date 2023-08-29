using Ronin.Grammar;
using Ronin.Hierarchy;

namespace Ronin.Compiler;

internal static partial class Analyzer
{
    public static void Define(Context context, Context definition, List<Error> errors)
    {
        definition.Parent = context;

        foreach (var statement in definition)
        {
            if (statement is Export export)
            {
                errors.Add(Error.ScopeMustBeAnonymous(definition, export));
                continue;
            }
            Define(definition, statement, errors);
        }
    }

    public static void Define(Context context, Scope scope, List<Error> errors)
    {
        scope.Definition.Parent = context;
        
        Identifier name = null;
        
        foreach (var statement in scope.Definition)
        {
            if (statement is Export export)
            {
                Export(scope, export, ref name, errors);
                continue;
            }
            Define(scope.Definition, statement, errors);
        }
        
        if (name is not null)
        {
            Global.Scope.Add(name, scope.Definition);            
        }
    }

    private static void Define(Context definition, Statement statement, List<Error> errors)
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

    private static void Export(Scope scope, Export export, ref Identifier identifier, List<Error> errors)
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

        if (identifier is not null)
        {
            errors.Add(Error.ScopeIsAlreadyPartOfAModule(scope.Definition, export));
            error = true;
        }
        
        if (error is false) identifier = export.Identifier;
    }

    private static void Import(Context definition, Import import)
    {
        definition.Imports.Add(new Context.Unresolved { Import = import });
    }

    private static void Define(Context definition, Function.Declaration declaration, List<Error> errors)
    {
        Define(definition, declaration.Definition, errors);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition
        };

        if (definition.Add(declaration.Identifier, function) is Error error) errors.Add(error);
    }

    private static void Define(Context definition, Datatype.Declaration declaration, List<Error> errors)
    {
        Define(definition, declaration.Definition, errors);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        if (definition.Add(declaration.Identifier, datatype) is Error error) errors.Add(error);
    }

    private static void Define(Context definition, Datum.Declaration declaration, List<Error> errors) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        if (definition.Add(declaration.Identifier, datum) is Error error) errors.Add(error);
    }
}
