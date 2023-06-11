using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Context : Semantic
{
    public static Context Global { get; } = new();

    internal static Dictionary<Words, Context> Named { get; } = new();

    internal HashSet<Words> Imports { get; } = new(ReferenceEqualityComparer.Instance);
    internal Dictionary<Identifier.Part, List<Semantic>> Children { get; } = new();

    protected internal Context() { }

    public Context(Definition definition, Context context, bool canBeNamed = false, List<Instruction> instructions = null)
    {
        Context = context;

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
                        instructions.Add(new UnresolvedAssignment(assignment) { Context = this });
                    }
                    break;

                case Reference reference:
                    if (instructions is null)
                    {
                        Errors.Add(new InstructionNotAllowedHere { Statement = statement });
                    }
                    else
                    {
                        instructions.Add(new UnresolvedInstruction(reference) { Context = this });
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
                                instructions.Add(new UnresolvedInstruction(instruction) { Context = this });
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

    public Context Add(in Identifier identifier, in Semantic semantic)
    {
        if (identifier.Parts.Count is 0)
        {
            Errors.Add(new AnonymousIdentifier { Statement = semantic.Source as Statement });
            return this;
        }

        var parts = CollectionsMarshal.AsSpan(identifier.Parts);
        return Add(parts, semantic);
    }

    public List<Semantic> Find(in Reference reference)
    {
        var components = CollectionsMarshal.AsSpan(reference.Components);
        var found = Find(components);
        if (found is not null) return found;
        
        Errors.Add(new DeveloperMistakeUnhandledSubclass<Reference.Component> { Statement = reference });
        return new();
    }

    private Context Add(in ReadOnlySpan<Identifier.Part> name, in Semantic semantic)
    {
        if (name.IsEmpty) return null;

        if (name.Length is not 1)
        {
            Context child = new();
            Children.Add(name[0], new() { child });
            return child.Add(name[1..], semantic);
        }

        if (Children.TryGetValue(name[0], out var list) is false)
        {
            list = new();
            Children.Add(name[0], list);
        }
        list.Add(semantic);
        return this;
    }

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference)
    {
        if (reference.IsEmpty) return new() { };

        var isLeaf = reference.Length is 1;

        Words words = reference[0];
        if (words is not null) 
        {
            if (Children.TryGetValue(words, out var children))
            {
                if (isLeaf) return children;
                List<Semantic> found = new(children.Count);
                foreach (var child in children)
                {
                    if (child is Context context) found.AddRange(context.Find(reference[1..]));
                }
                return found;
            }
        }

        Anonymous anonymous = reference[0];
        if (anonymous is not null) return Find(reference, anonymous);

        Interval interval = reference[0];
        if (interval is not null) return Find(reference, interval);

        return null;
    }

    private List<Semantic> Find(in ReadOnlyMemory<Reference.Component> reference, Words words)
    {
        throw new NotImplementedException();
    }

    private List<Semantic> Find(in ReadOnlyMemory<Reference.Component> reference, in Span<char> name)
    {
        throw new NotImplementedException();
    }

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference, Anonymous anonymous)
    {
        throw new NotImplementedException();
    }

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference, Interval interval)
    {
        throw new NotImplementedException();
    }

    private Words Export(Export export)
    {
        if (Named.TryAdd(export.Name, this) is false) Named[export.Name].Merge(this);
        return export.Name;
    }

    private void Declare(FunctionDeclaration function)
    {
        UnresolvedFunction unresolved = new(function);
        Identifier identifier = new(function.Name);
        unresolved.Context = Add(identifier, unresolved);
    }

    private void Declare(DatatypeDeclaration datatype)
    {
        UnresolvedDatatype unresolved = new(datatype);
        Identifier identifier = new(datatype.Name);
        unresolved.Context = Add(identifier, unresolved);
    }

    private void Declare(DatumDeclaration datum)
    {
        UnresolvedDatum unresolved = new(datum);
        Identifier identifier = new(datum.Name);
        unresolved.Context = Add(identifier, unresolved);
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