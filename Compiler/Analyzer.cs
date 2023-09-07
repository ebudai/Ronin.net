using Ronin.Grammar;
using Ronin.Hierarchy;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal class Analyzer
{
    public Module Global { get; } = new();
    public List<Error> Errors { get; } = new();

    public void Define(Context definition)
    {
        foreach (var statement in definition)
        {
            if (statement is Export export)
            {
                Errors.Add(Error.ScopeMustBeAnonymous(definition, export));
                continue;
            }
            Define(definition, statement);
        }
    }

    public void Define(Context parent, Scope scope)
    {
        scope.Definition.Parent = parent;
        Identifier name = null;
        
        foreach (var statement in scope.Definition)
        {
            if (statement is Export export)
            {
                Export(scope, export, ref name);
                continue;
            }
            Define(scope.Definition, statement);
        }
        
        if (ReferenceEquals(parent, Global) || name is not null)
        {
            Global.Add(scope.Definition, name);
        }
    }

    private void Define(Context definition, Statement statement)
    {
        switch (statement)
        {
            case Import import: definition.Import(import); break;
            case Function.Declaration function: Define(definition, function); break;
            case Datatype.Declaration datatype: Define(definition, datatype); break;
            case Datum.Declaration datum: Define(definition, datum); break;
            case Scope inner: Define(definition, inner); break;
            case Unknown unknown: Errors.Add(Error.UnknownSyntax(unknown)); break;
            default: break;
        }
    }

    private void Export(Scope scope, Export export, ref Identifier identifier)
    {
        bool error = false;

        if (scope is not AnonymousScope)
        {
            Errors.Add(Error.ScopeMustBeAnonymous(scope.Definition, export));
            error = true;
        }

        if (scope.Modifiers.Source.IsEmpty is false)
        {
            Errors.Add(Error.ScopeMustBeUnmodified(scope.Definition, export));
            error = true;
        }

        if (identifier is not null)
        {
            Errors.Add(Error.ScopeIsAlreadyPartOfAModule(scope.Definition, export));
            error = true;
        }
        
        if (error is false) identifier = export.Identifier;
    }

    private void Define(Context definition, Function.Declaration declaration)
    {
        declaration.Definition.Parent = definition;
        Define(declaration.Definition);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition,
        };

        if (definition.Add(declaration.Identifier, function) is Error error) Errors.Add(error);
    }

    private void Define(Context parent, Datatype.Declaration declaration)
    {
        declaration.Definition.Parent = parent;
        Define(declaration.Definition);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        if (parent.Add(declaration.Identifier, datatype) is Error error) Errors.Add(error);
    }

    private void Define(Context definition, Datum.Declaration declaration) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        if (definition.Add(declaration.Identifier, datum) is Error error) Errors.Add(error);
    }
}
