using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Context : Semantic
{
    public static Context Global { get; } = new();

    internal static Dictionary<Words, Context> Named { get; } = new();

    internal Context Parent { get; set; }
    internal HashSet<Words> Imports { get; } = new(ReferenceEqualityComparer.Instance);
    internal Dictionary<Identifier, List<Semantic>> Children { get; } = new();

    protected internal Context() { }

    public Context(Definition definition, Context context, bool canBeNamed = false, List<Instruction> instructions = null)
    {
        Parent = context;

        Words name = null;

        foreach (var statement in definition.Values)
        {
            switch (statement)
            {
                case Export export:
                    if (canBeNamed is false)
                    {
                        Errors.Add(new CannotJoinNamedContext { Statement = export });
                    }
                    else if (name is not null)
                    {
                        Errors.Add(new ContextAlreadyNamed { Statement = export });
                    }
                    else
                    {
                        name = Export(export);
                    }
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
                    // this could be a function call with useless brackets
                    if (anonymous is Inputs and { Values: [var input] })
                    {
                        Value value = input;
                        if (value is Reference instruction)
                        {
                            if (instructions is null)
                            {
                                Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                            }
                            else
                            {
                                instructions.Add(new UnresolvedInstruction(instruction, context));
                            }
                        }                        
                    }
                    else
                    {
                        Errors.Add(new NotAnInstruction { Statement = statement });
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
                    Errors.Add(new DeveloperMistakeUnhandledSubclass<Statement> { Statement = statement });
                    break;
            }
        }

        if (name is not null)
        {
            if (Named.TryGetValue(name, out var named))
            {
                named.Merge(this);
            }
            else
            {
                Named.Add(name, this);
            }
        }
    }

    public void Add(Identifier identifier, Semantic semantic)
    {
        if (Children.TryGetValue(identifier, out var children) is false)
        {
            children = new();
            Children.Add(identifier, children);
        }
        children.Add(semantic);
    }

    public Semantic Find(ReadOnlySpan<Reference.Component> reference)
    {
        throw new NotImplementedException();
    }

    private Words Export(Export export)
    {
        if (Named.TryAdd(export.Name, this) is false) Named[export.Name].Merge(this);
        return export.Name;
    }

    private Identifier GetPartialIdentifier(Name.Component component)
    {
        Words words = component;
        if (words is not null) return new Identifier(words);

        Parameters parameters = component;
        var data = new Datum[parameters.Values.Count];
        for (int i = 0, max = data.Length; i != max; ++i) data[i] = new UnresolvedDatum(parameters.Values[i], this);
        return new Identifier(data);
    }

    private void Declare(FunctionDeclaration function)
    {
        Context context = this;
        foreach (var component in function.Name.Components)
        {
            var identifier = GetPartialIdentifier(component);

            if (identifier is null)
            {
                Errors.Add(new UnknownSyntax { Statement = function });
                continue;
            }

            Context subcontext = ReferenceEquals(component, function.Name.Components[^1]) ? new UnresolvedFunction(function, context) : new Context();
            context.Add(identifier, subcontext);
            context = subcontext;
        }
    }

    private void Declare(DatatypeDeclaration datatype)
    {
        Context context = this;
        foreach (var component in datatype.Name.Components)
        {
            var identifier = GetPartialIdentifier(component);

            if (identifier is null)
            {
                Errors.Add(new UnknownSyntax { Statement = datatype });
                continue;
            }

            Context subcontext = ReferenceEquals(component, datatype.Name.Components[^1]) ? new UnresolvedDatatype(datatype, context) : new Context();
            context.Add(identifier, subcontext);
            context = subcontext;
        }
    }

    private void Declare(DatumDeclaration datum)
    {
        Words name = datum.Name.Components[0];
        Identifier identifier = new(name);
        Add(identifier, new UnresolvedDatum(datum, this));
    }

    private void Merge(Context context)
    {
        foreach (var (identifier, subcontexts) in context.Children)
        {
            if (Children.TryGetValue(identifier, out var children) is false)
            {
                children = new();
                Children.Add(identifier, children);
            }
            children.AddRange(subcontexts);
        }
    }
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