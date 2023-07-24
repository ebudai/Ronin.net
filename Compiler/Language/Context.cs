using Ronin.Grammar;

namespace Ronin.Language;

/*internal class Context : Semantic
{
    public static Context Global { get; } = new();
    public static Dictionary<Words, Context> Named { get; } = new();

    public Context Parent { get; init; }

    private HashSet<Words> Imports { get; } = new();    
    private Dictionary<Identifier.Part, List<Context>> Children { get; } = new();
    private Dictionary<Identifier.Part, List<Semantic>> Contents { get; } = new();
    private List<Instruction> Instructions { get; } = new();

    public bool IsEmpty => Children.Count is 0 && Contents.Count is 0;

    public Context Define(Definition definition, bool canBeNamed = false)
    {
        Context context = new() { Parent = this };

        Words name = null;

        foreach (var statement in definition.Values)
        {
            context.Errors.AddRange(statement switch
            {
                Export export => context.Join(export, ref name, canBeNamed),
                Import import => context.Use(import),
                FunctionDeclaration function => context.Declare(function),
                DatatypeDeclaration datatype => context.Declare(datatype),
                Assignment assigment => context.Assign(assigment),
                Reference reference => context.Call(reference),
                Anonymous value => context.Call(value),
                Grammar.Datum datum => context.Declare(datum),
                Scope scope => context.Call(scope),
                Unknown unknown => Error.UnknownSyntax(statement),
                _ => Error.UnhandledSubclass<Statement>(statement)
            });
        }

        if (name is not null)
        {
            if (Named.TryGetValue(name, out var named))
            {
                named.Merge(context);
                return named;
            }
            
            Named.Add(name, context);            
        }

        return context;
    }

    public List<Error> Add(Identifier identifier, Semantic semantic, Statement statement)
    {
        if (identifier.Parts.Count is 0) return Error.AnonymousIdentifier(statement);

        Add(identifier, semantic);
        return Error.None;
    }

    public List<Semantic> Find(Reference reference)
    {
        var found = Find(reference, 0) ?? new();
        if (found.Count is 0 && Parent is not null)
        {
            found.AddRange(Parent.Find(reference));
        }
        return found;
    }

    private void Add(Identifier identifier, Semantic semantic, int depth = 0)
    {
        if (identifier.Parts.Count < depth) return;

        var name = identifier.Parts[depth];

        if (identifier.Parts.Count == depth + 1)
        {            
            if (Contents.TryGetValue(name, out var contents) is false)
            {
                contents = new();
                Contents.Add(name, contents);
            }
            contents.Add(semantic);
            return;
        }

        if (Children.TryGetValue(name, out var children) is false)
        {
            children = new();
            Children.Add(name, children);
        }        
        
        Context child = new() { Parent = this };
        children.Add(child);
        child.Add(identifier, semantic, depth + 1);
    }

    private List<Error> Join(Export export, ref Words name, bool canBeNamed)
    {
        if (canBeNamed is false) return Error.CannotJoinNamedContext(export);
        if (name is not null) return Error.ContextAlreadyNamed(export);

        if (Named.TryAdd(export.Name, this) is false) Named[export.Name].Merge(this);

        name = export.Name;
        return Error.None;
    }

    private List<Error> Use(Import import) => Imports.Add(import.Name) ? Error.None : Error.ModuleAlreadyImported(import);

    private List<Error> Declare(Function.Declaration declaration)
    {
        Identifier identifier = new(declaration.Name, this);
        Function function = new(declaration, this);
        return Add(identifier, function, declaration);
    }

    private List<Error> Declare(Datatype.Declaration declaration)
    {
        Identifier identifier = new(declaration.Name, this);
        Datatype datatype = new(declaration, this);
        return Add(identifier, datatype, declaration);
    }

    private List<Error> Declare(Grammar.Datum declaration)
    {
        Identifier identifier = new(new Name(declaration.Name), this);
        Datum unresolved = new(declaration, this);
        Instructions.Add(new InitializeDatum(unresolved));
        return Add(identifier, unresolved, declaration);
    }

    private List<Error> Assign(Assignment assignment)
    {
        Instructions.Add(new SetValue(assignment, this));
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

        return Error.ValuesCannotBeStatements(value);
    }

    private List<Error> Call(Scope scope)
    {
        List<Error> errors = new();
        Context context = Define(scope.Definition, canBeNamed: true);
        if (scope.IsCompiled) errors.AddRange(context.Compile());
        Instructions.AddRange(context.Instructions);
        return errors;
    }

    private List<Semantic> Find(Reference reference, int depth)
    {
        if (reference.Components.Count >= depth) return new();

        Words words = reference.Components[depth];
        if (words is not null)
        {
            if (reference.Components.Count == depth + 1)
            {
                return Contents.TryGetValue(words, out var contents) ? contents : new();
            }
            
            if (Children.TryGetValue(words, out var children))
            {
                List<Semantic> found = new(children.Count);
                foreach (var child in children) found.AddRange(child.Find(reference, depth + 1));
                return found;
            }
        }

        Anonymous anonymous = reference.Components[depth];
        if (anonymous is not null) return Find(reference, anonymous, depth);

        return new();
    }

    private List<Semantic> Find(Reference reference, Anonymous anonymous, int depth) => anonymous switch
    {
        Inline literal => Find(reference, literal, depth),
        Inputs inputs => Find(reference, inputs, depth),
        _ => new(),
    };

    private List<Semantic> Find(Reference reference, Inline literal, int depth)
    {
        if (reference.Components.Count >= depth) return new();

        Result result = new(literal, null);

        if (reference.Components.Count == depth + 1)
        {            
            return Contents.TryGetValue(result, out var contents) ? contents : new();
        }

        if (Children.TryGetValue(result, out var children))
        {
            List<Semantic> found = new(children.Count);
            foreach (var child in children) found.AddRange(child.Find(reference, depth + 1));
            return found;
        }

        return new();
    }

    private List<Semantic> Find(Reference reference, Inputs inputs, int depth)
    {
        if (reference.Components.Count >= depth) return new();

        Results results = new(inputs, null);

        if (reference.Components.Count == depth + 1)
        {
            return Contents.TryGetValue(results, out var contents) ? contents : new();
        }

        if (Children.TryGetValue(results, out var children))
        {
            List<Semantic> found = new(children.Count);
            foreach (var child in children) found.AddRange(child.Find(reference, depth + 1));
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
        foreach (var semantics in Contents.Values)
        {
            foreach (var semantic in semantics)
            {
                if (semantic is Datum datum) datum.IsCompiled = true;
            }
        }
        return Error.None;
    }
}

internal partial class Error
{
    public static List<Error> ContextAlreadyNamed(Statement statement) => new() { new ContextAlreadyNamed { Statement = statement } };
    public static List<Error> EmptyContextName(Statement statement) => new() { new EmptyContextName { Statement = statement } };
    public static List<Error> CannotJoinNamedContext(Statement statement) => new() { new CannotJoinNamedContext { Statement = statement } };
    public static List<Error> ModuleAlreadyImported(Statement statement) => new() { new ModuleAlreadyImported { Statement = statement } };
    public static List<Error> ValuesCannotBeStatements(Statement statement) => new() { new ValuesCannotBeStatements { Statement = statement } };
    public static List<Error> UnknownSyntax(Statement statement) => new() { new UnknownSyntax { Statement = statement } };
}

internal class ContextAlreadyNamed : Error { }
internal class EmptyContextName : Error { }
internal class CannotJoinNamedContext : Error { }
internal class ModuleAlreadyImported : Error { }
internal class ValuesCannotBeStatements : Error { }
internal class UnknownSyntax : Error { }*/