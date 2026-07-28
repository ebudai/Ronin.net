// Copyright © 2026 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System.Collections.Generic;
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
    private Compilation() { }

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

    public IReadOnlyList<Finding> Findings => findings;

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
    private Declarations Scope(IReadOnlyList<Statement> statements, Declarations enclosing)
    {
        var declared = Declarations.Of(statements, Source, enclosing);

        foreach (var problem in declared.Problems) Add(problem);

        foreach (var body in statements.SelectMany(Bodies))
        {
            Scope(body, declared);
        }

        return declared;
    }

    /// <summary>
    ///     The scopes a statement opens. A body is a scope of its own: it sees
    ///     everything enclosing it and nothing it declares escapes, which for a
    ///     language where a declaration is a grammar production is the difference
    ///     between nesting and rewriting a sibling's syntax.
    /// </summary>
    private static IEnumerable<IReadOnlyList<Statement>> Bodies(Statement statement)
    {
        // An error node is a Function or a Type or a Scope too, and carries none
        // of the parts the real one would, so each of these can be absent.
        switch (statement)
        {
            case Grammar.Function { Definition.Statements: { } body }: yield return body; break;
            case Grammar.Type { Members: { } members }: yield return [.. members]; break;
            case Grammar.Scope scope: yield return scope.Statements; break;
            default: break;
        }
    }

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
    ///     Every type reached here is a named one — a declaring type, or the
    ///     runtime type of something a node held — and a named type has a
    ///     namespace. Handling a null one would be a branch nothing can take.
    /// </remarks>
    private static bool IsSyntax(System.Type type)
        => type.Namespace.StartsWith(Syntax, System.StringComparison.Ordinal);

    /// <summary>
    ///     The readable members of a node that could hold part of the tree.
    /// </summary>
    ///
    /// <remarks>
    ///     Declared BY the grammar, which is the filter that matters: nothing a
    ///     node inherits from the framework can hold a syntax node, and reaching
    ///     for one of those anyway finds members that cannot be read at all.
    ///     <c>Reference.Span</c> returns a <c>ReadOnlySpan</c>, and a by-ref-like
    ///     value cannot be boxed — reflection throws rather than answering, so
    ///     they are excluded by what they are and not by catching the attempt.
    /// </remarks>
    private static System.Reflection.PropertyInfo[] Members(System.Type type) => members.GetOrAdd(type, Reflect);

    private static System.Reflection.PropertyInfo[] Reflect(System.Type type)
        => [.. type.GetProperties(System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance)
                   .Where(property => property.CanRead
                                   && property.GetIndexParameters().Length is 0
                                   && property.PropertyType.IsByRefLike is false
                                   && IsSyntax(property.DeclaringType))];

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
        => Add(new Malformed(Where(error), error.Reason, Text(error)));

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
