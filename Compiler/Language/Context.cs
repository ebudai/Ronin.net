using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Context
{
    public static Context Global { get; } = new(null);
    public static Dictionary<Words, Context> Named { get; } = new();

    public HashSet<Words> Imports { get; } = new(ReferenceEqualityComparer.Instance);
    public Context Parent { get; }
    public Dictionary<Identifier.Part, List<Context>> Children { get; } = new();
    public Dictionary<Identifier.Part, List<Semantic>> Contents { get; } = new();
    public List<Instruction> Instructions { get; } = new();

    private Context(Context parent) => Parent = parent;

    public Context(Definition definition, Context parent, bool canBeNamed = false)
    {
        Parent = parent;

        Words name = null;

        foreach (var statement in definition.Values)
        {
            var errors = statement switch
            {
                Export export => Join(export, ref name, canBeNamed),
                Import import => Use(import),
                FunctionDeclaration function => Declare(function),
                DatatypeDeclaration datatype => Declare(datatype),
                Assignment assigment => Assign(assigment),
                Reference reference => Call(reference),
                Anonymous value => Call(value),
                DatumDeclaration datum => Declare(datum),
                Scope scope => Call(scope),
                Unknown unknown => new() { new UnknownSyntax { Statement = statement } },
                _ => new() { new DeveloperMistakeUnhandledSubclass<Statement> { Statement = statement } }
            };            
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

    public List<Error> Add(Identifier identifier, Semantic semantic)
    {
        if (identifier.Parts.Count is 0)
        {
            return new() { new AnonymousIdentifier { Statement = semantic.Source as Statement } };
        }
        
        var parts = CollectionsMarshal.AsSpan(identifier.Parts);
        return Add(parts, semantic);        
    }

    public List<Semantic> Find(Reference reference)
    {
        var components = CollectionsMarshal.AsSpan(reference.Components);
        var found = Find(components) ?? new();
        if (found.Count is 0 && Parent is not null)
        {
            found.AddRange(Parent.Find(reference));
        }
        return found;
    }

    private List<Error> Add(in ReadOnlySpan<Identifier.Part> name, Semantic semantic)
    {
        if (name.IsEmpty) return Error.None;

        if (name.Length is 1)
        {
            if (Contents.TryGetValue(name[0], out var contents) is false)
            {
                contents = new();
                Contents.Add(name[0], contents);
            }
            contents.Add(semantic);
            return Error.None;
        }

        if (Children.TryGetValue(name[0], out var children) is false)
        {
            children = new();
            Children.Add(name[0], children);
        }        
        
        Context child = new(this);
        children.Add(child);
        return child.Add(name[1..], semantic);        
    }

    private List<Error> Join(Export export, ref Words name, bool canBeNamed)
    {
        if (canBeNamed is false)
        {
            return new() { new CannotJoinNamedContext { Statement = export } };
        }

        if (name is not null)
        {
            return new() { new ContextAlreadyNamed { Statement = export } };
        }

        if (Named.TryAdd(export.Name, this) is false) Named[export.Name].Merge(this);
        name = export.Name;
        return Error.None;
    }

    private List<Error> Use(Import import) => Imports.Add(import.Name) ? Error.None : new() { new ModuleAlreadyImported { Statement = import } };

    private List<Error> Declare(FunctionDeclaration declaration)
    {
        Identifier identifier = new(declaration.Name, this);
        Function function = new(declaration, this);
        return Add(identifier, function);
    }

    private List<Error> Declare(DatatypeDeclaration declaration)
    {
        Identifier identifier = new(declaration.Name, this);
        Datatype datatype = new(declaration, this);
        return Add(identifier, datatype);
    }

    private List<Error> Declare(DatumDeclaration declaration)
    {
        Identifier identifier = new(declaration.Name, this);
        Datum unresolved = new(declaration, this);
        Instructions.Add(new InitializeDatum(unresolved));
        return Add(identifier, unresolved);
    }

    private List<Error> Assign(Assignment assignment)
    {
        Instructions.Add(new AssignmentInstruction(assignment, this));
        return Error.None;
    }

    private List<Error> Call(Reference reference)
    {
        Instructions.Add(new FunctionCall(reference, this));
        return Error.None;
    }

    private List<Error> Call(Value value)
    {
        if (value is Inputs inputs and { Values.Count: 1 })
        {
            Value possibleFunctionCall = inputs.Values[0];
            if (possibleFunctionCall is Reference reference)
            {
                Instructions.Add(new FunctionCall(reference, this));
                return Error.None;
            }
        }

        return new() { new ValuesCannotBeStatements { Statement = value } };
    }

    private List<Error> Call(Scope scope)
    {
        List<Error> errors = new();
        Context context = new(scope.Definition, this, canBeNamed: true);
        if (scope.IsCompiled) errors.AddRange(context.Compile());
        Instructions.AddRange(context.Instructions);
        return errors;
    }

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference)
    {
        if (reference.IsEmpty) return new();

        Words words = reference[0];
        if (words is not null)
        {
            if (reference.Length is 1)
            {
                return Contents.TryGetValue(words, out var contents) ? contents : new();
            }
            
            if (Children.TryGetValue(words, out var children))
            {
                List<Semantic> found = new(children.Count);
                foreach (var child in children) found.AddRange(child.Find(reference[1..]));
                return found;
            }
        }

        Anonymous anonymous = reference[0];
        if (anonymous is not null) return Find(reference, anonymous);

        return new();
    }

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference, Anonymous anonymous) => anonymous switch
    {
        Literal literal => Find(reference, literal),
        Inputs inputs => Find(reference, inputs),
        _ => new(),
    };

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference, Literal literal)
    {
        if (reference.IsEmpty) return new();

        Result result = new(literal, null);

        if (reference.Length is 1)
        {            
            return Contents.TryGetValue(result, out var contents) ? contents : new();
        }

        if (Children.TryGetValue(result, out var children))
        {
            List<Semantic> found = new(children.Count);
            foreach (var child in children) found.AddRange(child.Find(reference[1..]));
            return found;
        }

        return new();
    }

    private List<Semantic> Find(in ReadOnlySpan<Reference.Component> reference, Inputs inputs)
    {
        if (reference.IsEmpty) return new();

        Results results = new(inputs, null);

        if (reference.Length is 1)
        {
            return Contents.TryGetValue(results, out var contents) ? contents : new();
        }

        if (Children.TryGetValue(results, out var children))
        {
            List<Semantic> found = new(children.Count);
            foreach (var child in children) found.AddRange(child.Find(reference[1..]));
            return found;
        }

        return new();
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

    private List<Error> Compile()
    {
        List<Error> errors = new();
        foreach (var entry in Contents)
        {
            foreach (var semantic in entry.Value)
            {
                if (semantic is Datum datum)
                {
                    if (datum.IsCompiled)
                    {
                        errors.Add(new DatumIsAlreadyCompiled { Statement = datum.Source as Statement });
                        continue;
                    }
                    datum.IsCompiled = true;
                }
            }
        }
        return errors;
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