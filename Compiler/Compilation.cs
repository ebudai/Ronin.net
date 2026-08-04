// Copyright © 2026 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     One source file, from text to findings.
/// </summary>
///
/// <remarks>
///     <para>
///     The phases existed and nothing joined them. <c>Program</c> parsed, then
///     called <see cref="Declarations.Of"/> on the module's outermost statements
///     and stopped — so a duplicate declaration inside a <c>type</c> body was
///     never looked for, and every parse error below the top level was carried in
///     the tree and never read out of it. Two declarations of «x» inside one
///     «type Box» compiled with nothing to say.
///     </para>
///     <para>
///     The tests did not catch it because they reproduced the traversal
///     themselves: the nested-scope test parses two independent strings and hands
///     the second the first's <see cref="Declarations"/> by hand. That proves
///     merging works, which it does, and says nothing about whether anything
///     merges.
///     </para>
///     <para>
///     Everything a phase produces arrives as a <see cref="Finding"/>, parse
///     errors included. The alternative was for the executable to know about
///     <see cref="IError"/>, <c>Module.UnexpectedInputError</c> and the scope
///     rules separately, which is how it came to report exactly one of the three.
///     </para>
/// </remarks>
internal sealed class Compilation
{
    private Compilation() => Findings = new ReadOnlyCollection<Finding>(findings);

    public static Compilation Of(SourceText source)
    {
        Lexer lexer = new(source.Text);
        Parser parser = new(lexer.Lex());

        Compilation compilation = new() { Source = source, Module = parser.Parse() };

        compilation.Declare();

        return compilation;
    }

    public SourceText Source { get; private init; }

    public Module Module { get; private init; }

    /// <summary>The outermost scope's declarations, with the nested ones folded in.</summary>
    public Declarations Declarations { get; private set; }

    /// <remarks>
    ///     Wrapped, not handed over. «Program» chooses success or failure from
    ///     this count, so a caller that cast the read-only type back to the list
    ///     and cleared it made a malformed file compile clean — the declared
    ///     type was concealment rather than protection.
    /// </remarks>
    public IReadOnlyList<Finding> Findings { get; }

    private void Declare()
    {
        // Input no statement could account for. It is the module rather than a
        // statement, so no walk over statements would ever reach it.
        if (Module is Module.UnexpectedInputError unexpected) Malformed(unexpected);

        var statements = Module.Scopes[0].Statements;

        foreach (var error in Errors(Module)) Malformed(error);

        // A malformed subtree has nothing to declare. Declaration building
        // dereferences what the grammar promised — a parameter's identifier, a
        // pattern's blocks — and an error node carries none of it, so
        // «function f (var +) {}» reached Identifier.Named and killed the
        // process. Parse errors suppressing later phases is what lets every
        // later phase trust its input, and is why compilers have always done it.
        if (findings.Count is not 0)
        {
            Declarations = Declarations.Of([], Source);
            return;
        }

        Declarations = Scope(statements, enclosing: null);
    }

    /// <summary>
    ///     One scope's declarations, and then every scope it contains — each of
    ///     which sees this one, because a merged table is what makes a lookup one
    ///     probe rather than a walk up the chain.
    /// </summary>
    private Declarations Scope(IReadOnlyList<Statement> statements, Declarations enclosing,
                               Identifier variable = null, IReadOnlyList<Identifier> parameters = null,
                               string inside = null)
    {
        var declared = Declarations.Of(statements, Source, enclosing, variable, parameters);

        foreach (var problem in declared.Problems) Add(problem);

        // «inside» is null exactly where a «when» belongs — the module, and a
        // type's members. Everywhere else it names what the reader would call
        // the enclosing thing, and doubles as the reason there is one.
        if (inside is not null)
        {
            foreach (var misplaced in statements.OfType<Grammar.Scope>())
            {
                if (misplaced.Reacts) Add(new MisplacedWhen(Where(misplaced.Opened), inside));
            }
        }

        foreach (var body in statements.SelectMany(Bodies))
        {
            Scope(body.Statements, declared, body.Variable, body.Parameters, body.Inside);
        }

        return declared;
    }

    /// <summary>The span of one token, for a finding that points at a keyword.</summary>
    private Span Where(Token token) => Source.Span(token.Offset, token.Memory.Length);

    /// <summary>
    ///     The scopes a statement opens. A body is a scope of its own: it sees
    ///     everything enclosing it and nothing it declares escapes, which for a
    ///     language where a declaration is a grammar production is the difference
    ///     between nesting and rewriting a sibling's syntax.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A WALK, for the same reason the error walk is one. This was a switch
    ///     over the statement itself, and a delegate is a <c>Value</c> — so its
    ///     body could sit in a datum's initialiser, an input, a list, a lookup, a
    ///     parameter's default or another delegate, and none of those is a
    ///     statement. Every declaration diagnostic vanished inside one: «var
    ///     callback = (x) =&gt; { var d =&gt; Number; var d =&gt; Number; };»
    ///     compiled clean, while the same duplicate anywhere else is Shadowed.
    ///     Syntax diagnostics worked, because the error walk was already
    ///     complete; declaration diagnostics silently did not.
    ///     </para>
    ///     <para>
    ///     Ownership stays explicit. A node that opens a scope yields its body
    ///     and the walk does not descend into it — the recursion in
    ///     <see cref="Scope"/> does that, with this scope as the enclosing one.
    ///     What the walk does keep descending is everything BESIDE the body: a
    ///     function's parameter defaults can hold delegates of their own.
    ///     </para>
    /// </remarks>
    private static IEnumerable<Body> Bodies(Statement statement)
    {
        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        Stack<object> pending = new();

        pending.Push(statement);

        while (pending.Count is not 0)
        {
            var node = pending.Pop();

            if (seen.Add(node) is false) continue;

            // An error node is a Function or a Type or a Scope too, and carries
            // none of the parts the real one would, so each part can be absent.
            switch (node)
            {
                // A function's body is its parameters' scope. They were never
                // declared into it at all: nothing but the loop variable was,
                // so «type Box { var name; function read (name) {} }» had no
                // shadowing to report and «name» in the body would have read
                // the member.
                case Grammar.Function { Definition: { } definition } function:
                    seen.Add(definition);
                    yield return new Body(definition.Statements, null, Bound(function.Identifier),
                                          Named(function.Identifier));
                    break;

                case Grammar.Delegate { Definition: { } body } lambda:
                    seen.Add(body);
                    yield return new Body(body.Statements, null, Bound(lambda.Data), "a delegate");
                    break;

                // A type's members are where a «when» belongs, along with the
                // module: it lives as long as the instance does.
                case Grammar.Type { Members: { } members }:
                    yield return new Body([.. members], null, [], null);
                    continue;

                // a loop binds its variable in its body and nowhere else
                case Grammar.Scope.Iterating loop:
                    yield return new Body(loop.Statements, loop.Current, [], "a loop");
                    continue;

                case Grammar.Scope scope:
                    yield return new Body(scope.Statements, null, [], scope.Reacts ? "another «when»" : "a block");
                    continue;

                default:
                    break;
            }

            foreach (var child in Children(node)) pending.Push(child);
        }
    }

    /// <summary>The identifiers a parameter block declares, in order.</summary>
    private static IReadOnlyList<Identifier> Bound(Grammar.Parameters parameters)
        => [.. parameters.Select(parameter => parameter.AsDatum.Identifier)];

    /// <summary>
    ///     The same for a delegate, whose parameter may be a bare name.
    /// </summary>
    ///
    /// <remarks>
    ///     «(x) =&gt; …» and «x =&gt; …» declare «x» exactly as «(x =&gt;
    ///     Number)» does; only the typing is absent. A name is wrapped so that
    ///     one declaration path serves all three, rather than a second one
    ///     growing beside it with its own idea of the rules.
    /// </remarks>
    private static IReadOnlyList<Identifier> Bound(Grammar.Delegate.Parameters parameters)
        => [.. parameters.Select(Declaring)];

    private static Identifier Declaring(Grammar.Delegate.Parameter parameter)
    {
        if (parameter.AsDatum is Grammar.Datum declared) return declared.Identifier;

        Identifier wrapped = new();
        wrapped.Add(parameter.AsName);

        return wrapped;
    }

    /// <summary>The identifiers every parameter block of an identifier declares.</summary>
    private static IReadOnlyList<Identifier> Bound(Identifier identifier)
        => [.. identifier.Where(component => component.AsParameters is not null)
                         .SelectMany(component => Bound(component.AsParameters))];

    /// <param name="Inside">
    ///     What a reader would call this scope, or null where it is one a «when»
    ///     may be declared in. Only the module and a type's members are null.
    /// </param>
    private readonly record struct Body(IReadOnlyList<Statement> Statements, Identifier Variable,
                                        IReadOnlyList<Identifier> Parameters, string Inside);

    /// <summary>A declaration as a message would quote it.</summary>
    private static string Named(Identifier identifier) => $"«{identifier.Words}»";

    /// <summary>
    ///     Every error node anywhere in the tree.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Reflective, and deliberately so. The hand-written walk this replaces
    ///     descended scope bodies and carried a comment claiming an error node
    ///     could only ever sit in a statement position. That was wrong — a lookup
    ///     holds associations, an association holds values, a delegate holds
    ///     parameters, and an identifier holds parameter blocks, and every one of
    ///     those slots can hold a recovery node. «var value = { key = };»
    ///     compiled clean and «function f (var +) {}» killed the process.
    ///     </para>
    ///     <para>
    ///     A switch over node types would have the same defect the next time the
    ///     grammar grows a slot, and the failure mode is silence followed by a
    ///     crash two phases later. Walking every readable member cannot miss one.
    ///     This runs once per file over a tree of a few thousand nodes and feeds
    ///     diagnostics rather than any hot path, which is what makes reflection
    ///     an acceptable price for a completeness guarantee.
    ///     </para>
    /// </remarks>
    private static IEnumerable<IError> Errors(object root)
    {
        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        Stack<object> pending = new();
        pending.Push(root);

        while (pending.Count is not 0)
        {
            // never null: Children yields only what it has already established
            // is a syntax node, and the root is the module
            var node = pending.Pop();

            if (seen.Add(node) is false) continue;

            if (node is IError error) yield return error;

            foreach (var child in Children(node)) pending.Push(child);
        }
    }

    /// <summary>Whatever a node holds that is itself part of the tree.</summary>
    /// <summary>
    ///     Whether anything WITHIN this node — at any depth, not merely
    ///     directly beneath it — failed to parse.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Within, and the node itself is not asked. This is a node asking about
    ///     what it just built, so it cannot be an error yet — it is asking in
    ///     order to decide whether to become one. Naming it for that rather than
    ///     testing the root anyway, because a check no caller can reach is a
    ///     branch that has to be either covered by a fiction or excluded by an
    ///     attribute, and both hide that the shape is the invariant.
    ///     </para>
    ///     <para>
    ///     The same walk the diagnostic pass uses, asked as a question. A
    ///     shallower test — is this element itself an error — was written for
    ///     the collection classifier and covers only what is directly beneath
    ///     it, so an error one level down was still classified over and
    ///     replaced. Every wrapper that wants this question has to ask it
    ///     through the members the grammar declares, which is the whole reason
    ///     that walk is reflective.
    ///     </para>
    /// </remarks>
    internal static bool BrokenWithin(object node)
    {
        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        Stack<object> pending = new();

        // The root's children, not the root. A node asking this about ITSELF has
        // not answered yet — it is asking in order to have an answer — so
        // consulting the cache here would read the field it is about to fill.
        foreach (var child in Children(node)) pending.Push(child);

        while (pending.Count is not 0)
        {
            var next = pending.Pop();

            if (seen.Add(next) is false) continue;
            if (next is IError) return true;

            // A node that already knows about its own subtree ENDS the descent.
            // Without this each nested collection re-walked everything beneath
            // it as it finished, which is the same total answer at d² the cost.
            if (next is IAnswersBroken answered)
            {
                if (answered.Broken) return true;

                continue;
            }

            foreach (var child in Children(next)) pending.Push(child);
        }

        return false;
    }

    private static IEnumerable<object> Children(object node)
    {
        // An aggregate IS its elements and no property exposes them — the
        // indexer takes an argument — so a lookup's associations and a
        // parameter block's parameters are reachable only this way.
        if (node is System.Collections.IEnumerable sequence)
        {
            foreach (var element in sequence)
            {
                if (IsSyntax(element)) yield return element;
            }
        }

        foreach (var member in Members(node.GetType()))
        {
            var value = member.GetValue(node);

            if (IsSyntax(value)) yield return value;

            // a slot holding a collection of nodes — «Statements», «Imports»,
            // «Scopes» — which is a List and so not itself a syntax type
            else if (value is System.Collections.IEnumerable nested and not string)
            {
                foreach (var element in nested)
                {
                    if (IsSyntax(element)) yield return element;
                }
            }
        }
    }

    private static bool IsSyntax(object value) => value is not null && IsSyntax(value.GetType());

    /// <remarks>
    ///     A null namespace is a real answer and not a missing one. It used to
    ///     say here that every type reached is named and every named type has a
    ///     namespace — which is true of types anyone WRITES, and false of the
    ///     ones the compiler synthesises: a collection expression assigned to an
    ///     <c>IReadOnlyList</c> produces a read-only wrapper with no namespace at
    ///     all. The moment a grammar node exposed one of those, this walk threw
    ///     on every compilation.
    /// </remarks>
    private static bool IsSyntax(System.Type type)
        => type.Namespace?.StartsWith(Syntax, System.StringComparison.Ordinal) is true;

    /// <summary>
    ///     The readable members of a node that could hold part of the tree.
    /// </summary>
    ///
    /// <remarks>
    ///     Declared BY the grammar, which is the first filter: nothing a node
    ///     inherits from the framework can hold a syntax node, and reaching for
    ///     one of those anyway finds members that cannot be read at all.
    ///     <c>Reference.Span</c> returns a <c>ReadOnlySpan</c>, and a by-ref-like
    ///     value cannot be boxed — reflection throws rather than answering, so
    ///     they are excluded by what they are and not by catching the attempt.
    ///     <para>
    ///     And by TYPE, which is the second: a slot that could hold a child has
    ///     a type capable of holding one. This walk reads every member before
    ///     asking whether the answer is syntax, so a computed property was being
    ///     evaluated for its side effects on the clock — <c>Identifier.Writable</c>
    ///     renders the whole shape and lexes it back, and a «bool» has never held
    ///     a syntax node in its life. Every declaration in a file paid that
    ///     readback here and then again in <c>TryPattern</c>, and an over-width
    ///     declaration paid it in full BEFORE the width guard that exists to
    ///     bound exactly that work.
    ///     </para>
    ///     <para>
    ///     <c>Declares</c> and <c>Reads</c> were made methods for this same
    ///     reason and the rule was not applied to the rest. Filtering by type
    ///     applies it to all of them, and to the next one nobody remembers.
    ///     </para>
    /// </remarks>
    private static System.Reflection.PropertyInfo[] Members(System.Type type) => members.GetOrAdd(type, Reflect);

    private static System.Reflection.PropertyInfo[] Reflect(System.Type type)
        => [.. type.GetProperties(System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance)
                   .Where(property => property.CanRead
                                   && property.GetIndexParameters().Length is 0
                                   && property.PropertyType.IsByRefLike is false
                                   && IsSyntax(property.DeclaringType)
                                   && Holds(property.PropertyType))];

    /// <summary>
    ///     Whether a slot of this type could hold a syntax node, directly or as
    ///     the elements of something.
    /// </summary>
    ///
    /// <remarks>
    ///     Deliberately generous about what it admits and exact about what it
    ///     rejects: yielding a slot that turns out to hold nothing costs a
    ///     <c>GetValue</c>, and skipping one that holds a child would put this
    ///     walk back where the hand-written one was.
    /// </remarks>
    private static bool Holds(System.Type type) => Holds(type, []);

    /// <param name="seen">
    ///     What is already being asked about, because a type can be its own
    ///     element type: «string» implements «IComparable&lt;string&gt;», so
    ///     following element types without this recurses until the stack ends.
    /// </param>
    private static bool Holds(System.Type type, HashSet<System.Type> seen)
    {
        if (seen.Add(type) is false) return false;

        // Whether a NODE could be here, and not whether the slot's own type is
        // spelled in the grammar's namespace. Those are different questions: a
        // slot declared «IError» or «IParsable&lt;Statement&gt;» holds grammar
        // nodes and is declared elsewhere, so the namespace test would have kept
        // the walk from ever reading it — and this walk exists to make that
        // impossible. «object» needs no case of its own now; every node is
        // assignable to it.
        if (nodes.Any(type.IsAssignableFrom)) return true;

        // A collection is a child slot when its ELEMENTS could be children.
        // «IReadOnlyList&lt;Statement&gt;» is one; «IReadOnlyList&lt;string&gt;»
        // and «string[]» are the shapes of a computed answer, not of a tree.
        if (type.IsArray) return Holds(type.GetElementType(), seen);

        // From the ENUMERABLE contract, because that is the only generic
        // relationship that says what a collection holds. Reading every generic
        // argument of every interface and base instead got the common cases right
        // by luck and two others wrong: «class Children : ArrayList,
        // IComparable&lt;Children&gt;» is an untyped enumerable that can hold
        // anything, and its unrelated comparison supplied an argument that
        // answered the question before the untyped case was reached. In the other
        // direction «Func&lt;Statement&gt;» was admitted for being generic over a
        // syntax type, though nothing can enumerate it.
        var elements = Enumerated(type).ToArray();

        if (elements.Length is not 0) return elements.Any(element => Holds(element, seen));

        // Nothing said what it holds, so it could hold anything. «ArrayList» and
        // a bare «IEnumerable» are that case, and <see cref="Children"/> knows
        // how to enumerate both — answering false is what would keep it from ever
        // seeing the slot.
        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    ///     Every node the grammar can actually produce.
    /// </summary>
    ///
    /// <remarks>
    ///     Concrete only, because the question is what a slot could HOLD, and an
    ///     abstract type is never the runtime type of anything.
    /// </remarks>
    private static readonly System.Type[] nodes =
        [.. typeof(Compilation).Assembly
                               .GetTypes()
                               .Where(type => IsSyntax(type)
                                           && type.IsAbstract is false
                                           && type.IsInterface is false)];

    /// <summary>What this type says it enumerates, if it says.</summary>
    private static IEnumerable<System.Type> Enumerated(System.Type type)
        => type.GetInterfaces()
               .Append(type)
               .Where(each => each.IsGenericType
                           && each.GetGenericTypeDefinition() == typeof(IEnumerable<>))
               .Select(each => each.GetGenericArguments()[0]);

    private const string Syntax = "Ronin.Grammar";

    /// <summary>
    ///     Reflected members, cached across compilations.
    /// </summary>
    ///
    /// <remarks>
    ///     Concurrent, because the cache is process-wide and a compilation is
    ///     not. A plain dictionary with a check and then an assignment corrupted
    ///     itself the first time two files were compiled at once — five runs out
    ///     of five — and the CLI only escaped it by compiling one file at a time,
    ///     which is a property of today's loop and not of this type.
    /// </remarks>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Type, System.Reflection.PropertyInfo[]> members = new();

    private void Malformed(IError error)
    {
        // Recognised in order to be refused by name. Everything else here is a
        // parse that failed; this is one that succeeded at telling us what the
        // author meant, and said it is not built.
        if (error is Grammar.Type.ReactiveMemberError reactive)
        {
            Add(new WhenInType(Where(reactive.Opened)));
            return;
        }

        Add(new Malformed(Where(error), error.Reason, Text(error)));
    }

    /// <summary>
    ///     Records a finding, once.
    /// </summary>
    ///
    /// <remarks>
    ///     The scope rules run over the MERGED table, which is what lets an inner
    ///     declaration invalidate an outer name — and means a conflict between two
    ///     OUTER declarations is found again in every scope nested inside them. It
    ///     is the same finding each time, so saying so is enough: a finding
    ///     involving anything the inner scope added differs in its symbols or its
    ///     span and survives.
    /// </remarks>
    private void Add(Finding finding)
    {
        if (seen.Add(Identify(finding))) findings.Add(finding);
    }

    /// <summary>
    ///     What makes two findings the same finding.
    /// </summary>
    ///
    /// <remarks>
    ///     The message, because it already contains every role the finding
    ///     carries and is the thing a reader would see twice. This used to join
    ///     the roles out of a string dictionary with a literal NUL byte — which
    ///     compiled, and made the central file of the new pipeline binary to git,
    ///     to grep and to every reviewer.
    /// </remarks>
    private static (FindingKind Kind, int Offset, int Length, string Message) Identify(Finding finding)
        => (finding.Kind, finding.Primary.Offset, finding.Primary.Length, finding.Message);

    /// <summary>The offending source, canonically rendered.</summary>
    ///
    /// <remarks>
    ///     An error node always carries at least one token, because a production
    ///     that consumed nothing may not return a node at all — that is the
    ///     progress invariant, and indexing here rather than guarding is what
    ///     says so. An empty one would be that invariant broken, not a case.
    /// </remarks>
    private static string Text(IError error)
    {
        var tokens = error.Tokens.Span;

        return tokens[0].ToLexemes(tokens[^1].Next as Token).Render();
    }

    /// <summary>What the error node consumed, as a span.</summary>
    private Span Where(IError error)
    {
        var tokens = error.Tokens.Span;
        var start = tokens[0].Offset;
        var last = tokens[^1];

        return Source.Span(start, last.Offset + last.Memory.Length - start);
    }

    private readonly List<Finding> findings = [];
    private readonly HashSet<(FindingKind Kind, int Offset, int Length, string Message)> seen = [];
}
