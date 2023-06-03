using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Context : Semantic
{
    public static Context Global { get; } = new();

    private static Dictionary<Words, Context> Named { get; } = new();

    private Context Parent { get; set; }
    private HashSet<Words> Imports { get; } = new(ReferenceEqualityComparer.Instance);
    private Dictionary<Identifier, Datatype> Datatypes { get; } = new();
    private Dictionary<Identifier, Function> Functions { get; } = new();
    private Dictionary<Identifier, Datum> Data { get; } = new();

    private Context() { }

    public Context(Definition definition, Context context, bool canBeNamed = false, List<Instruction> instructions = null)
    {
        Parent = context;

        Words name = null;

        foreach (var statement in definition.Values)
        {
            switch (statement)
            {
                case Export export:
                    Export(export, ref name, canBeNamed);
                    break;

                case Import import:
                    if (Imports.Add(import.Name) is false) Errors.Add(new ModuleAlreadyImported { Statement = import });
                    break;

                case FunctionDeclaration function:
                    Declare(function);
                    break;

                case DatatypeDeclaration datatype:
                    Declare(datatype);
                    break;

                case Assignment assignment:
                    if (instructions is null)
                    {
                        Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                    }
                    else
                    {
                        instructions.Add(new UnresolvedAssignment(assignment, this));
                    }
                    break;

                case Reference reference:
                    if (instructions is null)
                    {
                        Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                    }
                    else
                    {
                        instructions.Add(new UnresolvedInstruction(reference, context));
                    }
                    break;

                case Anonymous anonymous:
                    if (anonymous is Inputs inputs
                        && inputs.Values.Count is 1
                        && inputs.Values[0] is Reference functionCall)
                    {
                        if (instructions is null)
                        {
                            Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                        }
                        else
                        {
                            instructions.Add(new UnresolvedInstruction(functionCall, context));
                        }
                    }
                    else
                    {
                        Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                    }
                    break;

                case DatumDeclaration datum:
                    Declare(datum);
                    break;

                case Scope scope:
                    if (instructions is null)
                    {
                        Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                    }
                    else
                    {
                        _ = new Context(scope.Definition, this, true, instructions);
                    }
                    break;

                case Unknown:
                    Errors.Add(new UnknownSyntax { Statement = statement });
                    break;

                default:
                    Errors.Add(new DeveloperMistakeUnhandledSubclassException<Statement> { Statement = statement });
                    break;
            }
        }

        if (name is not null)
        {
            if (Named.TryGetValue(name, out var named))
            {
                named.Merge(this, definition);
            }
            else
            {
                Named.Add(name, this);
            }
        }
    }

    private void Export(Export export, ref Words name, bool canBeNamed)
    {
        if (canBeNamed is false)
        {
            Errors.Add(new CannotJoinNamedContext { Statement = export });
            return;
        }

        if (name is not null)
        {
            Errors.Add(new ContextAlreadyNamed { Statement = export });
            return;
        }

        name = export.Name;

        if (Named.TryAdd(name, this) is false) Named[name].Merge(this, export);
    }

    private bool Contains(Identifier identifier)
        => Functions.ContainsKey(identifier)
        || Datatypes.ContainsKey(identifier)
        || Data.ContainsKey(identifier);

    private void Declare(FunctionDeclaration function)
    {
        Identifier identifier = new(function.Name, this);
        if (Contains(identifier))
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = function });
            return;
        }
        Functions.Add(identifier, new UnresolvedFunction(function, this));
    }

    private void Declare(DatatypeDeclaration datatype)
    {
        Identifier identifier = new(datatype.Name, this);
        var alreadyused = Contains(identifier);
        if (alreadyused && datatype.IsExtension is false)
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = datatype });
            return;
        }
        var unresolved = new UnresolvedDatatype(datatype, this);
        if (datatype.IsExtension && Datatypes.TryGetValue(identifier, out var existing))
        {
            existing.Definition.Merge(unresolved.Definition, datatype);
        }
        else if (alreadyused is false)
        {
            Datatypes.Add(identifier, unresolved);
        }
        else
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = datatype });
        }
    }

    private void Declare(DatumDeclaration datum)
    {
        Identifier identifier = new(datum.Name, this);
        if (Contains(identifier))
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = datum });
            return;
        }
        Data.Add(identifier, new Datum(datum, this));
    }

    private void Merge(Context context, Statement statement)
    {
        foreach (var datatype in context.Datatypes)
        {
            var unresolved = datatype.Value as UnresolvedDatatype;
            var extension = (unresolved.Source as DatatypeDeclaration).IsExtension;
            var alreadyused = Contains(datatype.Key);
            if (alreadyused && extension is false)
            {
                Errors.Add(new IdentifierAlreadyExists { Statement = statement });
                return;
            }
            if (extension && Datatypes.TryGetValue(datatype.Key, out var existing))
            {
                existing.Definition.Merge(unresolved.Definition, statement);
            }
            else if (alreadyused is false)
            {
                Datatypes.Add(datatype.Key, datatype.Value);
            }
            else
            {
                Errors.Add(new IdentifierAlreadyExists { Statement = statement });
            }
        }

        foreach (var function in context.Functions)
        {
            if (Contains(function.Key))
            {
                Errors.Add(new IdentifierAlreadyExists { Statement = statement });
                return;
            }
            Functions.Add(function.Key, function.Value);
        }

        foreach (var datum in context.Data)
        {
            if (Contains(datum.Key))
            {
                Errors.Add(new IdentifierAlreadyExists { Statement = statement });
                return;
            }
            Data.Add(datum.Key, datum.Value);
        }
    }
    /*public Context(Scope scope, Context parent, bool canBeNamed = false)
    {
        foreach (var statement in scope.Values)
        {
            

            switch (statement)
            {

                case DatatypeDeclaration datatypeDeclaration:
                    context.Declare(datatypeDeclaration);
                    break;

                case Scope procedure:
                    Declare(procedure, context);
                    break;

                case Assignment assignment:
                    context.Declare(assignment);
                    break;

                case Reference reference:
                    context.Declare(reference);
                    break;

                case Anonymous anonymous:
                    context.Declare(anonymous);
                    break;

                default: break;
            }
        }

        return context;
    }

    private static Context Declare(Export export, Context context)
    {
        var parent = Global;
        for (int i = 0, max = export.Name.Source.Length - 1; i != max; ++i)
        {
            var name = export.Name.Source.Span[i].ToString();
            Context child = new() { Parent = parent };
            parent = context.Modules.GetOrAdd(name, _ => child);
        }
        context.Parent = parent;
        var key = export.Name.Source.Span[^1].ToString();
        return context.Modules.AddOrUpdate(key, context, (_, old) => old.Incorporate(context));
    }

    private void Declare(Import import) => Imports.Add(new Context(import.Name, this));

    private void Declare(FunctionDeclaration declaration)
    {
        if (Find(declaration.Identifier) is Context function)
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = declaration, Existing = function });
            return;
        }

        Functions.Add(declaration.Identifier, new Function(declaration, this));
    }

    private void Declare(DatatypeDeclaration declaration)
    {
        if (Find(declaration.Identifier) is Context datatype)
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = declaration, Existing = datatype });
            return;
        }

        Datatypes.Add(declaration.Identifier, new Datatype(declaration, this));
    }

    private void Declare(Assignment assignment) => Instructions.Add(new Instruction(assignment, this));

    private void Declare(Anonymous anonymous)
    {
        switch (anonymous)
        {
            case Grammar.Delegate @delegate:
                Declare(@delegate);
                break;

            default: break;
        }
    }

    private void Declare(Grammar.Delegate @delegate)
    {
        var context = Declare(@delegate.Body, this);
        foreach (var datum in @delegate.Data) context.Declare(datum);        
    }

    private void Declare(DatumDeclaration declaration)
    {
        if (Find(declaration.Name) is Context existing)
        {
            Errors.Add(new IdentifierAlreadyExists { Statement = declaration, Existing = existing });
            return;
        }

        Datum datum = new(declaration, this)
        {
            Datatype = new UnresolvedDatatype()
        };
        Data.Add(declaration.Name, datum);
    }

    private Context GetModule(ReadOnlySpan<Token> name)
    {
        if (name.IsEmpty) return null;

        if (Modules.TryGetValue(name[0].sourcecode.ToString(), out var module) is false) return null;

        return name.Length is 1 ? module : module.GetModule(name[1..]);
    }

    private Error Add(KeyValuePair<Identifier, Function> function)
    {
        if (Functions.TryGetValue(function.Key, out var existing))
        {
            return new IdentifierAlreadyExists { Statement = function.Value.Source as Statement, Existing = existing };
        }
        Functions.Add(function.Key, function.Value);
        return null;
    }

    public Error Add(KeyValuePair<Identifier, Datatype> datatype)
    {
        if (Datatypes.TryGetValue(datatype.Key, out var existing))
        {
            return new IdentifierAlreadyExists { Statement = datatype.Value.Source as Statement, Existing = existing };
        }
        Datatypes.Add(datatype.Key, datatype.Value);
        return null;
    }

    public Error Add(KeyValuePair<Identifier, Datum> datum)
    {
        if (Data.TryGetValue(datum.Key, out var existing))
        {
            return new IdentifierAlreadyExists { Statement = datum.Value.Source as Statement, Existing = existing };
        }
        Data.Add(datum.Key, datum.Value);
        return null;
    }

    public Context Find(Identifier identifier)
    {
        if (Functions.TryGetValue(identifier, out var function)) return function;
        if (Datatypes.TryGetValue(identifier, out var datatype)) return datatype;
        return Data.TryGetValue(identifier, out var datum) ? datum : null;        
    }

    private Context()
    {
        Parent = null;
    }

    private Context Incorporate(Context from)
    {
        foreach (var function in from.Functions)
        {
            if (Add(function) is Error error) from.Errors.Add(error);
        }
        
        foreach (var datatype in from.Datatypes)
        {
            if (Add(datatype) is Error error) from.Errors.Add(error);
        }
        
        foreach (var datum in from.Data)
        {
            if (Add(datum) is Error error) from.Errors.Add(error);
        }

        return this;
    }*/
}

[ExcludeFromCodeCoverage]
internal class ContextAlreadyNamed : Error { }

[ExcludeFromCodeCoverage]
internal class EmptyContextName : Error { }

[ExcludeFromCodeCoverage]
internal class CannotJoinNamedContext : Error { }

[ExcludeFromCodeCoverage]
internal class ModuleAlreadyImported : Error { }

[ExcludeFromCodeCoverage]
internal class ValuesCannotBeStatements : Error { }

[ExcludeFromCodeCoverage]
internal class UnknownSyntax : Error { }