// Copyright © 2026 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System;
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

    /// <param name="module">
    ///     The identity the source's types are rooted in, supplied by whoever owns the
    ///     document (VARIABLE-AND-MODULE Q5). A saved file's owner may leave it unset
    ///     and let the path stand for it; an editor must pass a token that belongs to
    ///     the open BUFFER and survives every edit, or a type's identity would change
    ///     on each keystroke. The one-shot compiler, having no document across
    ///     snapshots, mints a fresh buffer identity for a pathless source with none.
    /// </param>
    public static Compilation Of(SourceText source, ModuleIdentity module = null)
    {
        Lexer lexer = new(source.Text);
        Parser parser = new(lexer.Lex());

        Compilation compilation = new()
        {
            Source = source,
            Module = parser.Parse(),
            Identity = module ?? (source.Path is { } path ? new ModuleIdentity.Path(path)
                                                          : new ModuleIdentity.Buffer(new object())),
        };

        compilation.Declare();

        return compilation;
    }

    public SourceText Source { get; private init; }

    public Module Module { get; private init; }

    /// <summary>The identity this source's types are rooted in — its path, its buffer, or one supplied.</summary>
    private ModuleIdentity Identity { get; init; }

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
                               string inside = null, bool reacting = false, Container container = null,
                               bool named = true, IReadOnlyList<Body> ancillary = null,
                               Grammar.Function function = null)
    {
        // The container of a type is a STRUCTURE — the module it is in, then a segment
        // per enclosing named scope — compared as one rather than a joined string a
        // module or a segment could collide on (CONTAINER-IDENTITY-RULING §3). Its
        // root is the module's IDENTITY, a type and not a string, settled at the entry
        // point from the owner's handle or the source's path (VARIABLE-AND-MODULE Q5).
        container ??= new Container(Identity, []);

        // A type declaration belongs to its nearest named container, not the block
        // it sits in (SCOPE-IDENTITY-RULING, H). So a named container also declares
        // the types written in its transparent sub-scopes — where they are nameable
        // and where a second of one name is «Shadowed» — and a transparent scope
        // declares none of its own, seeing the container's merged in. Only the
        // declaration set moves; every other phase reads the statements as written.
        //
        // A container's ancillary scopes — a function's parameter-default delegates —
        // are transparent too, so their types belong to it as much as its body's do
        // (REAUDIT55 finding 2), gathered in before the walk stops at the container.
        List<Grammar.Type> lifted = [];

        if (ancillary is not null)
            foreach (var scope in ancillary) lifted.AddRange(TypesOf(scope));

        IEnumerable<Statement> declaring = named
            ? statements.Concat(statements.SelectMany(Hoisted)).Concat(lifted)
                        .OrderBy(statement => statement is Grammar.Member { Identifier: { } identifier }
                                     ? identifier.Span(Source).Offset
                                     : int.MaxValue)
            : statements.Where(statement => statement is not Grammar.Type);

        var declared = Declarations.Of(declaring, Source, enclosing, variable, parameters, container);

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

        List<Reading> read = [];

        foreach (var reading in Read(statements, declared))
        {
            readings.Add(reading);
            read.Add(reading);

            if (reading.Resolution.Kind is ResolutionKind.Ambiguous)
            {
                Add(new Ambiguous(reading.Span,
                                  [.. reading.Resolution.Readings],
                                  reading.Repairs,
                                  reading.Resolution.Total,
                                  reading.Resolution.Bounded));
            }
        }

        foreach (var finding in Annotations(statements, declared, function)) Add(finding);

        foreach (var finding in Initializers(statements, declared)) Add(finding);

        if (function is not null)
            foreach (var finding in Returns(function, declared, read)) Add(finding);

        // The signature's SORTS resolved against this function's own table as well, and
        // stored back on its declaration — which sees the whole container, not the null
        // a body-local type left before this table existed (REAUDIT56 finding 2).
        // Published to BOTH the enclosing registration AND this body's own inherited
        // copy: a deeper body reads THIS table, so leaving its copy stale makes a nested
        // same-shape declaration compare against the pre-body slot and read an overload
        // where the two in fact share one sort (REAUDIT60 finding 1). Found by the span
        // it was registered under, not by its pattern: reading the pattern back
        // constructs, which a width-bound identifier refuses by throwing, and a function
        // whose pattern was refused is registered nowhere, so the search misses it.
        if (function is not null)
        {
            var where = function.Identifier.Span(Source);

            foreach (var overloads in new[] { enclosing.Overloads, declared.Overloads })
                foreach (var registered in overloads.Values)
                    for (var index = 0; index < registered.Count; index++)
                        if (registered[index].Span == where)
                            registered[index] = declared.Resolved(registered[index]);
        }

        foreach (var finding in Exits(reacting)) Add(finding);

        // Bodies grouped by the REGISTERED overload they own, not by their rendered
        // segment. The B ruling joins the bodies of one overload SET — established by
        // the pattern the declaration pass recorded, matched here by the span it left
        // — not every body whose identifier renders the same words, least of all one
        // that pass refused to register at all (REAUDIT58 finding 1). A block, loop,
        // delegate, or «when» is transparent (Container null) and recursed on its own;
        // a type body, or a function refused registration, owns no such pattern and is
        // recursed on its own too, so its diagnostics remain while it joins no set.
        Dictionary<Pattern, List<Body>> sets = [];
        List<Body> independent = [];

        foreach (var body in statements.SelectMany(Bodies))
        {
            if (body.Container is null)
            {
                Scope(body.Statements, declared, body.Variable, body.Parameters, body.Inside, body.Reacts,
                      container, named: false, ancillary: body.Ancillary, function: body.Function);

                continue;
            }

            var pattern = body.Function is { } owner
                ? declared.Overloads.FirstOrDefault(entry =>
                      entry.Value.Any(signature => signature.Span == owner.Identifier.Span(Source))).Key
                : null;

            if (pattern is null)
            {
                independent.Add(body);

                continue;
            }

            if (sets.TryGetValue(pattern, out var siblings) is false) sets[pattern] = siblings = [];

            siblings.Add(body);
        }

        foreach (var body in independent)
            Scope(body.Statements, declared, body.Variable, body.Parameters, body.Inside, body.Reacts,
                  container.Within(body.Container), named: true, ancillary: body.Ancillary, function: body.Function);

        foreach (var (pattern, bodies) in sets)
        {
            var nested = container.Within(bodies[0].Container);

            // One local declaration is its container's whole type table already.
            // Several of one registered shape are ONE container (CONTAINER-IDENTITY-
            // RULING, B): a type in any is visible across all, so they share one type
            // table, and each body resolves its own signature against it (REAUDIT57
            // finding 1). Either way the bodies leave their resolved signatures on the
            // container's declarations, for the classifier below.
            if (bodies.Count is 1)
            {
                var body = bodies[0];

                Scope(body.Statements, declared, body.Variable, body.Parameters, body.Inside, body.Reacts, nested,
                      named: true, ancillary: body.Ancillary, function: body.Function);
            }
            else
            {
                // Every body's and ancillary's types, declared once, in source order: a
                // repeat across two is «Shadowed» here — H's uniqueness across the
                // container, reported once.
                List<Grammar.Type> types = [];

                foreach (var body in bodies) types.AddRange(TypesOf(body));

                var shared = Declarations.Of(types.OrderBy(type => type.Identifier.Span(Source).Offset), Source,
                                             declared, container: nested);

                foreach (var problem in shared.Problems) Add(problem);

                // Resolve EVERY owner's signature against the complete shared table and
                // publish it to both the shared and the declaring table BEFORE any body
                // recurses — so a nested declaration in an earlier body sees a later
                // sibling's OWNING sort, not its pre-body null slot (REAUDIT61 finding
                // 1). Each body's own resolution below re-does its signature the same
                // way, idempotently; what matters is that no sibling is still stale when
                // a deeper scope reads the set.
                foreach (var body in bodies)
                {
                    var where = body.Function.Identifier.Span(Source);

                    foreach (var overloads in new[] { shared.Overloads, declared.Overloads })
                        foreach (var registered in overloads.Values)
                            for (var index = 0; index < registered.Count; index++)
                                if (registered[index].Span == where)
                                    registered[index] = shared.Resolved(registered[index]);
                }

                foreach (var body in bodies)
                    Scope(body.Statements, shared, body.Variable, body.Parameters, body.Inside, body.Reacts, nested,
                          named: false, ancillary: body.Ancillary, function: body.Function);
            }

            // Classify the VISIBLE candidate set — this scope's registered declarations
            // of the pattern plus the ones merged in from enclosing scopes — because a
            // LOCAL declaration is the trigger and the visible count is the set, not the
            // number of bodies in this statement list (REAUDIT59 finding 1). Each
            // candidate carries the sorts resolved at its OWN owner, which «Classify»
            // compares rather than resolving the words again here (finding 2).
            if (declared.Overloads[pattern].Count > 1)
                foreach (var finding in declared.Classify(pattern)) Add(finding);
        }

        // The ancillary scopes are transparent and belong to THIS container: recursed
        // with its declarations enclosing them and its container as their own, their
        // types already lifted into it, so a delegate in a parameter default sees the
        // container's types and a use of its name outside the container does not.
        if (ancillary is not null)
            foreach (var scope in ancillary)
                Scope(scope.Statements, declared, scope.Variable, scope.Parameters, scope.Inside, scope.Reacts,
                      container, named: false);

        return declared;
    }

    /// <summary>
    ///     What each of this scope's statements can be read as.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     The join between the two halves of the frontend, which had not been
    ///     made: the resolver was reachable only from its own tests, so an
    ///     ambiguous statement in a real file produced no finding and could not
    ///     fail a build. Every rule that refuses a name at its declaration exists
    ///     to keep this error answerable, and none of them was answering to
    ///     anything.
    ///     </para>
    ///     <para>
    ///     THIS scope's table and not an enclosing one, because a nested body
    ///     declares into its own — so the walk stops at every body and the
    ///     recursion below picks it up with the right symbols. Stopping at the
    ///     body's STATEMENT LIST instead never stopped at all: the walk yields a
    ///     collection's elements and never the collection, so the node compared
    ///     against was one it could not reach, and a body's statements were read
    ///     twice wherever the enclosing table could also read them.
    ///     </para>
    ///     <para>
    ///     NOT A TYPE, which is the other thing the walk stops at. A type
    ///     annotation is a reference too — «=> list of number» is a run of words
    ///     awaiting a meaning exactly as a statement is — so the walk read every
    ///     one of them against the VALUE table, where they mean nothing. Mostly
    ///     that produced a no-reading nobody reports; where the annotation's
    ///     words happened to be ambiguous as values, it reported an ambiguity
    ///     about a type, quoting readings that were never in question.
    ///     <para>
    ///     Types resolve against a table that does not exist yet, and reading
    ///     them against the wrong one is worse than not reading them at all.
    ///     </para>
    ///     </para>
    ///     <para>
    ///     Only ambiguity, for now. A span with no reading at all is the other
    ///     half and wants its own message: "no reading" covers an undeclared
    ///     name, a call that does not fit, and a phase this compiler has not
    ///     built yet, and reporting them as one thing would say the wrong one
    ///     most of the time.
    ///     </para>
    /// </remarks>
    private IEnumerable<Reading> Read(IReadOnlyList<Statement> statements, Declarations declared)
    {
        Resolver resolver = new(declared.Symbols);

        foreach (var statement in statements)
        {
            // The OUTERMOST reference and no further. A bracketed part is a
            // reference of its own, so «(send a to b) + (send a to b)» held
            // three — the whole expression and each half — and reported one
            // mistake three times at three spans. The whole expression's
            // readings already contain every combination of its parts', and
            // they are the ones a reader would bracket.
            foreach (var reference in Walk<Reference>(statement,
                                                      into: node => node is not Grammar.Scope
                                                                 && node is not Grammar.Type
                                                                 && node is not Reference))
            {
                var lexemes = reference.ToLexemes();
                var resolution = resolver.Resolve(lexemes);

                // Searched only where there is something to repair. The search
                // resolves a candidate per subspan, which is affordable on an
                // error path and would not be on every statement in a file.
                yield return new Reading(reference.Where(Source),
                                         resolution,
                                         resolution.Kind is ResolutionKind.Ambiguous
                                       ? Repairs.For(resolver, lexemes, resolution)
                                       : []);
            }
        }
    }

    /// <summary>
    ///     What each type annotation in this scope resolves to, and where it does
    ///     not.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     The other half of <see cref="Read"/>, and the comment it closes. A type
    ///     annotation is a reference too — «=> list of number» is a run of words
    ///     awaiting a meaning exactly as a statement is — and until now it was read
    ///     against the VALUE table, where it means nothing, or not read at all. It
    ///     resolves against the same table now, filtered to the type kind: the
    ///     value side is <c>Known</c>/<c>Callable</c>, this is that filter read the
    ///     other way, in one pass rather than a second table.
    ///     </para>
    ///     <para>
    ///     THIS scope's table and no further, for the reason <see cref="Read"/>
    ///     stops where it does: the walk halts at a nested body — a <c>Scope</c> or
    ///     a type's <c>Definition</c> — because a parameter's or a return's type
    ///     belongs to the signature written here, while a member's belongs to the
    ///     body and the recursion reaches it with the body's own symbols. It
    ///     descends INTO a <c>Type.Unresolved</c>, which the value walk skips,
    ///     because that is the annotation it is here to read.
    ///     </para>
    ///     <para>
    ///     BOTH halves are reported, where the value side reports only ambiguity.
    ///     A no-reading has one cause here — the words are not a type, because the
    ///     table is complete at the annotation — so <see cref="UnknownType"/> says
    ///     the one true thing rather than guessing among several. And a type with
    ///     more than one reading is an ambiguity like any other: the function-type
    ///     arrow does not associate, so «text => number => truth» and a two-arrow
    ///     lookup are ties the reader brackets, with the same finding and the same
    ///     repairs the value side already produces.
    ///     </para>
    /// </remarks>
    private IEnumerable<Finding> Annotations(IReadOnlyList<Statement> statements, Declarations declared,
                                             Grammar.Function signature)
    {
        Resolver resolver = new(declared.Symbols, kind: SymbolKind.Type);

        // A function's parameter and return annotations belong to its OWN container
        // and are resolved here, against its table — which holds the types its body
        // and ancillary scopes declare — not against the scope that declares it
        // (SCOPE-IDENTITY-RULING; REAUDIT56 finding 2). The enclosing walk stops at a
        // function; this scope walks that function's signature, everything but its
        // body, which is already this scope's own statements.
        List<object> roots = [.. statements];

        if (signature is not null)
            foreach (var child in Children(signature))
                if (ReferenceEquals(child, signature.Definition) is false)
                    roots.Add(child);

        foreach (var root in roots)
        {
            foreach (var annotation in Walk<Grammar.Type.Unresolved>(root,
                         into: node => node is not Grammar.Scope && node is not Grammar.Type.Definition
                                    && node is not Grammar.Function))
            {
                var lexemes = annotation.Reference.ToLexemes();
                var resolution = resolver.Resolve(lexemes);
                var where = annotation.Reference.Where(Source);

                if (resolution.Kind is ResolutionKind.Ambiguous)
                {
                    yield return new Ambiguous(where,
                                               [.. resolution.Readings],
                                               Repairs.For(resolver, lexemes, resolution),
                                               resolution.Total,
                                               resolution.Bounded);
                }
                else if (resolution.Kind is ResolutionKind.NoParse)
                {
                    yield return new UnknownType(where, lexemes.Render());
                }
                else if (resolution.TryTree(out var tree))
                {
                    // Resolved to one tree: the semantic type the annotation names,
                    // returned and kept for the checks that will read it against a
                    // value's own. «Sort.Of» is null where the tree resolves but is
                    // an arity-wrong group a later pass refuses — kept as null rather
                    // than dropped, so the span is still recorded.
                    types.Add(new Annotation(where, Sort.Of(tree, declared.ContainerOf)));
                }
                else
                {
                    // Past the resolver's ceiling, so no tree and no sort. Reported
                    // rather than dropped: silence here reads to a later pass as an
                    // omitted annotation, not an over-limit one.
                    yield return new OversizeType(where);
                }
            }
        }
    }

    /// <summary>
    ///     Where a datum's initializer disagrees with its declared type — «var x =>
    ///     number = "text"».
    /// </summary>
    ///
    /// <remarks>
    ///     The join step 2 makes, and the other side of <see cref="Annotations"/>: the
    ///     annotation names a type and the initializer has a value, each resolved
    ///     against the same table the type walk and the value walk already use — the
    ///     type kind for the one, the value kind for the other — and compared by
    ///     EQUALITY, there being no subtyping. Only what both sides can name is checked:
    ///     a literal initializer, whose sort <see cref="Sort.Infer"/> reads, against a
    ///     resolved annotation. A value whose sort is not inferred yet, or an annotation
    ///     that names no type, leaves nothing to compare and is left to its own walk's
    ///     finding rather than reported twice.
    /// </remarks>
    private IEnumerable<Finding> Initializers(IReadOnlyList<Statement> statements, Declarations declared)
    {
        Resolver resolver = new(declared.Symbols, kind: SymbolKind.Type);

        foreach (var statement in statements)
        {
            foreach (var datum in Walk<Grammar.Datum>(statement,
                         into: node => node is not Grammar.Scope && node is not Grammar.Function
                                    && node is not Grammar.Type.Definition))
            {
                // Only a lone LITERAL this slice, whose sort its own token carries — a
                // reference initializer resolves to a tree «Sort.Infer» reads, and an
                // aggregate its elements do, each in its own increment.
                if (datum.Type is not Grammar.Type.Unresolved annotation
                    || datum.Initializer is not Grammar.Literal literal)
                {
                    continue;
                }

                var words = annotation.Reference.ToLexemes();
                resolver.Resolve(words).TryTree(out var tree);
                if (Sort.Of(tree, declared.ContainerOf) is not Sort expected) continue;

                var token = literal.Tokens.Span[0];
                if (Sort.Denoted(token as Lexicon.Literal) is Sort.Scalar actual && expected.Equals(actual) is false)
                {
                    var last = literal.Tokens.Span[^1];
                    yield return new TypeMismatch(Source.Span(token.Offset, last.Offset + last.Memory.Length - token.Offset),
                                                  actual.Name, words.Render());
                }
            }
        }
    }

    /// <summary>
    ///     Where a «return (_)» carries a value that is not the function's declared
    ///     return type — «function f => number { return "text"; }».
    /// </summary>
    ///
    /// <remarks>
    ///     The return-site half of step 2, reading the same «return (_)» calls
    ///     <see cref="Exits"/> finds for its exit-flavour check — the answer each carries,
    ///     inferred and compared by equality against the declared return, resolved against
    ///     this body's table as the signature's own sorts are. Only a literal answer this
    ///     slice, whose sort <see cref="Sort.Infer"/> reads: a valueless exit carries no
    ///     answer and is skipped, a return with no written type is inference and later, and
    ///     one whose type is unknown is its own annotation finding rather than this.
    /// </remarks>
    private IEnumerable<Finding> Returns(Grammar.Function function, Declarations declared, IReadOnlyList<Reading> body)
    {
        if (function.Returns is not Grammar.Type.Unresolved annotation) yield break;

        Resolver resolver = new(declared.Symbols, kind: SymbolKind.Type);

        var words = annotation.Reference.ToLexemes();
        resolver.Resolve(words).TryTree(out var tree);
        if (Sort.Of(tree, declared.ContainerOf) is not Sort expected) yield break;

        foreach (var exit in body.Where(reading => reading.Resolution.TryTree(out _)).SelectMany(Called))
        {
            if (Sort.Infer(exit.Answer) is Sort.Scalar actual && expected.Equals(actual) is false)
                yield return new TypeMismatch(Source.Span(exit.Answer.Offset, exit.Answer.Length), actual.Name, words.Render());
        }
    }

    /// <summary>
    ///     How this body leaves itself, and whether it agrees with itself.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     One concept at two arities: «return (_)» and bare «return» both mean
    ///     leave this body now, and differ in whether there is an answer to
    ///     carry. So a body has ONE exit flavour, decided by whether any
    ///     «return (_)» appears in it — and that is not a rule of its own. It is
    ///     the check that stops the return type having two answers, seen from
    ///     the other side.
    ///     </para>
    ///     <para>
    ///     The other half of the same collection is the INFERENCE: no «return
    ///     (_)» means the answer type is the action type, and some means unify
    ///     their arguments into it. That half waits for a type to unify into,
    ///     and this is the walk it will read.
    ///     </para>
    /// </remarks>
    private IEnumerable<Finding> Exits(bool reacting)
    {
        var answering = readings.Skip(exits)
                                .Where(reading => reading.Resolution.TryTree(out _))
                                .SelectMany(Called)
                                .ToArray();

        exits = readings.Count;

        // RESERVED everywhere and LEGAL only in a «when», which is why the
        // spelling is refused globally and the placement is reported here. A
        // «stop» outside one is the builtin misplaced rather than a word nobody
        // declared, and the message can say what was probably meant.
        foreach (var halt in answering.Where(exit => exit.Halts && reacting is false))
        {
            yield return new MisplacedStop(halt.Span);
        }

        var carrying = answering.Where(exit => exit.Answers).ToArray();

        // A reaction has nobody to answer, so only the valueless form is legal
        // in one. Reported at each site rather than once for the body, because
        // each is a separate edit.
        if (reacting)
        {
            foreach (var exit in carrying) yield return new AnsweringReaction(exit.Span);

            yield break;
        }

        if (carrying.Length is 0) yield break;

        foreach (var exit in answering.Where(exit => exit.Answers is false))
        {
            yield return new MixedExits(exit.Span);
        }
    }

    /// <summary>The exits one statement contains, at whatever depth.</summary>
    ///
    /// <remarks>
    ///     At DEPTH, because «return» is a call like any other and a call can sit
    ///     inside one. Looking only at the top of a statement would answer for
    ///     the shapes people write and stay silent on the ones they do not, which
    ///     is the wrong way round for a rule about legality.
    ///     <para>
    ///     At its OWN span, which it did not have. Every match yielded the whole
    ///     statement's, so «send (return 1) to (return 2)» reported one finding
    ///     over the whole expression rather than two at the two «return»s — and
    ///     one of them silently, because two findings sharing a kind, a span and
    ///     a message are recorded once. Two edits, one message, and the contract
    ///     three lines up says each site is separate precisely so each is a
    ///     separate edit.
    ///     </para>
    /// </remarks>
    private IEnumerable<(Span Span, bool Answers, bool Halts, Node Answer)> Called(Reading reading)
    {
        reading.Resolution.TryTree(out var tree);

        foreach (var node in tree.Whole)
        {
            if (node is not Node.Call call) continue;

            var at = Source.Span(call.Offset, call.Length);

            // «return (_)» carries its answer for the return-type check; the valueless
            // exits carry none, so a caller inferring the answer's sort skips them.
            if (call.Pattern.Equals(SymbolTable.Answer)) yield return (at, true, false, call.Arguments[0]);
            if (call.Pattern.Equals(SymbolTable.Exit)) yield return (at, false, false, null);
            if (call.Pattern.Equals(SymbolTable.Halt)) yield return (at, false, true, null);
        }
    }

    private int exits;

    /// <summary>
    ///     What one statement was read as, and where it sits.
    /// </summary>
    ///
    /// <remarks>
    ///     Kept rather than derived from the findings, because the reading that
    ///     matters most to a reader is the one that SUCCEEDED — "what did the
    ///     compiler think I wrote" is the question a candy grammar provokes, and
    ///     an unambiguous statement produces no finding to answer it from.
    /// </remarks>
    internal readonly record struct Reading
    {
        public Reading(Span span, Resolution resolution, IReadOnlyList<Repair> repairs)
        {
            Span = span;
            Resolution = resolution;
            Repairs = Owned.Copy(repairs);
        }

        public Span Span { get; }

        public Resolution Resolution { get; }

        /// <summary>The bracketings, owned where the reading is recorded.</summary>
        public IReadOnlyList<Repair> Repairs { get; }
    }

    /// <summary>Every statement's reading, in the scope that owns it.</summary>
    public IReadOnlyList<Reading> Readings => new ReadOnlyCollection<Reading>(readings);

    private readonly List<Reading> readings = [];

    /// <summary>
    ///     One resolved type annotation: where it sits, and the sort it names — null
    ///     where the words resolve but are a type-position group whose arity a later
    ///     pass refuses.
    /// </summary>
    internal readonly record struct Annotation(Span Span, Sort Type);

    /// <summary>Every resolved type annotation's sort, in the scope that owns it.</summary>
    internal IReadOnlyList<Annotation> Types => new ReadOnlyCollection<Annotation>(types);

    private readonly List<Annotation> types = [];

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
    private static IEnumerable<Body> Bodies(object root)
    {
        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        Stack<object> pending = new();

        pending.Push(root);

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
                                          Named(function.Identifier), Container: function.Identifier.Words,
                                          Ancillary: Signature(function, definition), Function: function);
                    continue;

                case Grammar.Delegate { Definition: { } body } lambda:
                    seen.Add(body);
                    yield return new Body(body.Statements, null, Bound(lambda.Data), "a delegate");
                    break;

                // A type's members are where a «when» belongs, along with the
                // module: it lives as long as the instance does.
                case Grammar.Type { Members: { } members } type:
                    yield return new Body([.. members], null, [], null, Container: type.Identifier.Words);
                    continue;

                // a loop binds its variable in its body and nowhere else
                case Grammar.Scope.Iterating loop:
                    yield return new Body(loop.Statements, loop.Current, [], "a loop");
                    continue;

                case Grammar.Scope scope:
                    yield return new Body(scope.Statements, null, [], scope.Reacts ? "another «when»" : "a block", scope.Reacts);
                    continue;

                default:
                    break;
            }

            foreach (var child in Children(node)) pending.Push(child);
        }
    }

    /// <summary>
    ///     A function's ancillary transparent scopes — the delegates its parameter
    ///     defaults hold — each belonging to the function, not the scope that holds it
    ///     (SCOPE-IDENTITY-RULING, H). Everything BESIDE the body is walked; the body
    ///     is the function's own scope and is recursed on its own.
    /// </summary>
    private static IReadOnlyList<Body> Signature(Grammar.Function function, object definition)
    {
        List<Body> scopes = [];

        foreach (var child in Children(function))
        {
            if (ReferenceEquals(child, definition)) continue;

            scopes.AddRange(Bodies(child));
        }

        return scopes;
    }

    /// <summary>
    ///     The type declarations in a statement's transparent sub-scopes — a block,
    ///     a loop, a delegate, a «when» — which belong to the nearest named
    ///     container above them (SCOPE-IDENTITY-RULING, H). A nested named container
    ///     is not descended: its own types are its own, and its name is collected as
    ///     a type of the scope that holds it, not walked into for more.
    /// </summary>
    private static IReadOnlyList<Grammar.Type> Hoisted(Statement statement)
    {
        List<Grammar.Type> types = [];

        foreach (var body in Bodies(statement).Where(body => body.Container is null))
        {
            types.AddRange(body.Statements.OfType<Grammar.Type>());

            foreach (var inner in body.Statements) types.AddRange(Hoisted(inner));
        }

        return types;
    }

    /// <summary>
    ///     The complete set of type declarations a body's container holds — its own,
    ///     those hoisted from its transparent sub-scopes, and those its ancillary
    ///     scopes (a function's parameter-default delegates) hold — all belonging to
    ///     it (SCOPE-IDENTITY-RULING, H). The same set the container is built from, so
    ///     the cross-body uniqueness check counts what the container declares
    ///     (REAUDIT56 finding 3).
    /// </summary>
    private static List<Grammar.Type> TypesOf(Body body)
    {
        List<Grammar.Type> types = [.. body.Statements.OfType<Grammar.Type>()];

        foreach (var statement in body.Statements) types.AddRange(Hoisted(statement));

        foreach (var scope in body.Ancillary ?? []) types.AddRange(TypesOf(scope));

        return types;
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
    /// <param name="Ancillary">
    ///     The transparent scopes in a named container's ANCILLARY syntax — a
    ///     function's parameter-default delegates — which belong to it, not to the
    ///     scope that holds it (SCOPE-IDENTITY-RULING, H). They ride with the body so
    ///     the container declares their types and encloses them, rather than the walk
    ///     yielding them flat to the container above.
    /// </param>
    /// <param name="Function">
    ///     The function this is the body of, where it is one — the source of the
    ///     parameter and return annotations, which belong to this container and are
    ///     resolved against its own table, not the scope that declares it
    ///     (SCOPE-IDENTITY-RULING; REAUDIT56 finding 2). Null for every other scope.
    /// </param>
    private readonly record struct Body(IReadOnlyList<Statement> Statements, Identifier Variable,
                                        IReadOnlyList<Identifier> Parameters, string Inside,
                                        bool Reacts = false, string Container = null,
                                        IReadOnlyList<Body> Ancillary = null, Grammar.Function Function = null);

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
    private static IEnumerable<IError> Errors(object root) => Walk<IError>(root, into: _ => true);

    /// <summary>
    ///     Every <typeparamref name="T"/> anywhere beneath <paramref name="root"/>,
    ///     by the same walk and for the same reason.
    /// </summary>
    ///
    /// <param name="into">
    ///     Whether to descend past a node. A scope's statements are resolved
    ///     against that scope's own table, so a walk gathering expressions has to
    ///     stop where a nested body begins — the error walk descends everywhere,
    ///     because a malformed node is malformed wherever it sits.
    /// </param>
    private static IEnumerable<T> Walk<T>(object root, Func<object, bool> into)
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

            if (node is T found) yield return found;

            if (into(node) is false) continue;

            foreach (var child in Children(node)) pending.Push(child);
        }
    }

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
    ///     declaration invalidate an outer name — and means a conflict between two OUTER
    ///     declarations is found again in every scope nested inside them. A finding is
    ///     dropped when one already recorded is the SAME conflict over the SAME
    ///     participants or a wider set of them: a nested scope re-finding an outer
    ///     conflict it adds nothing to, or a narrower slice of one already reported
    ///     whole (the deepest, widest classification is recorded first, so the module-
    ///     level slice of it is the one dropped). But a finding that names a declaration
    ///     none of the recorded ones do is a DISTINCT conflict and kept: two overloads
    ///     in sibling scopes share one inherited primary but each names its own local
    ///     declaration, and both are real (REAUDIT62 finding 1).
    /// </remarks>
    private void Add(Finding finding)
    {
        if (findings.Any(kept => kept.Kind == finding.Kind && kept.Message == finding.Message
                              && Participants(finding).IsSubsetOf(Participants(kept))))
            return;

        findings.Add(finding);
    }

    /// <summary>The source spans a finding is about: the one it points at, and every site alongside it.</summary>
    private static HashSet<(int Offset, int Length)> Participants(Finding finding)
        => [.. finding.Related.Select(related => (related.Span.Offset, related.Span.Length))
                              .Prepend((finding.Primary.Offset, finding.Primary.Length))];

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
}
