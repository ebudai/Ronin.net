// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Linq;
using Blocks = System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<string>>;

namespace Ronin.Grammar;

/// <summary>
///     What a parsed scope declares, as the resolver's scope.
/// </summary>
///
/// <remarks>
///     <para>
///     The join the resolver has been waiting for. Until now a
///     <see cref="SymbolTable"/> was built by hand in a test; this builds one from
///     what the parser found, so a statement can be resolved against the names and
///     patterns its own file declares.
///     </para>
///     <para>
///     Which of the two a declaration produces is structural rather than a matter
///     of interpretation: <c>var base price =&gt; Number</c> is all name words and
///     so is a name, while <c>function compute total for (order)</c> contains a
///     parameter block and so is a pattern with a hole where the block sits.
///     </para>
/// </remarks>
internal sealed class Declarations
{
    private Declarations() { }

    public SymbolTable Symbols { get; } = new();

    /// <summary>
    ///     What could not be declared, and what the scope-wide rules rejected.
    /// </summary>
    /// <remarks>
    ///     The grammar's own patterns join the declared ones, because R5 and R6
    ///     are about what is IN SCOPE and those are in every scope. Marked
    ///     inherited so that provenance blames the declaration a programmer
    ///     wrote rather than the language they wrote it in.
    /// </remarks>
    public IReadOnlyList<Finding> Problems => found ??= [.. problems.Concat(Rules.Validate(symbols,
        [
            .. SymbolTable.Builtins.Select(pattern => new Shape(pattern, source.Span(0, 0),
                                                               Inherited: true, Builtin: true)),
            .. shapes.SelectMany(shape => shape.Value.Select(shaped => new Shape(shape.Key, shaped.Span, shaped.Inherited))),
        ]))];

    /// <summary>
    ///     The declarations of one scope, folded into everything the enclosing
    ///     scopes declared.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Inward yes, outward no. An inner scope sees every enclosing
    ///     declaration at any position — the pre-pass already makes order
    ///     irrelevant within a scope and nesting inherits that — while nothing an
    ///     inner scope declares is visible to a sibling or a parent. That
    ///     direction matters more here than in most languages, because a pattern
    ///     declaration is a grammar production: escaping ones would let a nested
    ///     function change the grammar of its siblings' bodies, and scopes would
    ///     have to resolve inside-out.
    ///     </para>
    ///     <para>
    ///     The result is flat. Shadowing being an error is what allows that, and
    ///     it is why a lookup stays one probe rather than a walk up N levels.
    ///     </para>
    /// </remarks>
    /// <param name="variable">
    ///     A loop's variable, which is declared in the body and nowhere else.
    ///     It is a declaration site like any other and must be checked like one:
    ///     R5 rejecting «order in transit» is the whole reason «for each ... in
    ///     ...» is safe to spell, and a loop variable that skipped the check
    ///     would be the one hole in it.
    /// </param>
    public static Declarations Of(IEnumerable<Statement> statements, SourceText source,
                                  Declarations enclosing = null, Identifier variable = null,
                                  IReadOnlyList<Identifier> parameters = null, Container container = null)
    {
        Declarations declarations = new()
        {
            source = source,
            container = container ?? new Container(new ModuleIdentity.Path(string.Empty), []),
        };

        if (enclosing is not null)
        {
            declarations.Symbols.Merging(enclosing.Symbols);
            declarations.inherited.UnionWith(enclosing.Symbols.Names.Keys);

            foreach (var (pattern, declared) in enclosing.Overloads) declarations.Overloads[pattern] = [.. declared];
            foreach (var (name, span) in enclosing.written) declarations.written[name] = span;

            // Stamped on the way in, because it is the only provenance the rules
            // can have: they run over a merged table, where both sides of a
            // collision are simply "in scope" and nothing else says which was
            // written first.
            foreach (var (pattern, spans) in enclosing.shapes)
                declarations.shapes[pattern] = [.. spans.Select(shaped => shaped with { Inherited = true })];

            declarations.symbols.AddRange(enclosing.symbols.Select(symbol => symbol with { Inherited = true }));
        }

        if (variable is not null) declarations.Bind(variable);

        // Before the body's own statements, because that is where they are: a
        // parameter is declared on entry, and a body redeclaring one is
        // shadowing it. They were declared nowhere at all — flattened to strings
        // for the runtime and never presented to any rule — so «type Box { var
        // name; function read (name) {} }» had nothing to report, and «name» in
        // that body would have read the member.
        foreach (var parameter in parameters ?? []) declarations.Receive(parameter);

        foreach (var statement in statements) declarations.Declare(statement);

        // Once the whole table stands, resolve each LOCALLY DECLARED signature's
        // parameter and return spellings to their SORTS and store them beside the words
        // — a signature the checker can unify without resolving again (REAUDIT55
        // finding 3). Only the local ones, still unresolved from «Declare»: a signature
        // MERGED in from an enclosing scope already carries the sorts resolved at its
        // own owner, and re-reading its spellings here — in a container it was not
        // written in — would rebind its types to this one and collapse two distinct
        // named types into a false duplicate (REAUDIT59 finding 2).
        foreach (var pattern in declarations.Overloads.Keys.ToList())
            declarations.Overloads[pattern] =
                [.. declarations.Overloads[pattern].Select(signature =>
                    signature.ParameterSorts is null ? declarations.Resolved(signature) : signature)];

        // The overload set is NOT classified here. A shape's declarations share one
        // container (CONTAINER-IDENTITY-RULING, B), so their types are visible across
        // it and a signature naming one resolves only against the WHOLE container's
        // type table — which this single body's table is not (REAUDIT57 finding 1).
        // «Compilation.Scope» builds that shared table from every body of the shape
        // and classifies the set once against it, through «Classify».
        return declarations;
    }

    /// <summary>
    ///     The over-declaration findings for one shape, classified against THIS table
    ///     — its parameter sorts read where the whole overload container is visible.
    /// </summary>
    ///
    /// <remarks>
    ///     TWO RULES wearing one name until the type checker lands, and only one ever
    ///     expires. Declarations of a shape whose parameter types DIFFER wait for
    ///     type-directed selection; declarations whose types are the SAME wait for
    ///     nothing, because no type information could ever tell them apart.
    ///     <para>
    ///     GROUPED, not counted. A count told duplicate from overload for a pair and
    ///     nothing more: three declarations «A, A, B» collapsed to one duplicate
    ///     spanning the first and the last — not even the colliding pair — and the
    ///     overload between the A's and the B went unreported. A group of more than one
    ///     is a duplicate reported against the declarations that collide; two or more
    ///     groups remaining is an overload set, because removing a duplicate does not
    ///     make «A» and «B» choosable.
    ///     </para>
    /// </remarks>
    internal IReadOnlyList<Finding> Classify(Compiler.Pattern pattern)
    {
        List<Finding> found = [];

        // A pattern in the table is in «shapes» too — they are filled together. A
        // single declaration falls through to no finding on its own: one group of one
        // is neither a duplicate nor a choice.
        var groups = Overloads[pattern]
                     .Zip(shapes[pattern], (signature, shape) => (Key: Sorted(signature), shape.Span))
                     .GroupBy(entry => entry.Key, Keying.Comparer)
                     .ToList();

        foreach (var duplicate in groups.Where(group => group.Count() > 1))
        {
            var sites = duplicate.Select(entry => entry.Span).ToList();
            var finding = new DuplicateSignature(sites[0], pattern.ToString());

            foreach (var site in sites.Skip(1)) finding.Alongside(site, "also declared here");

            found.Add(finding);
        }

        if (groups.Count > 1)
        {
            var sites = groups.Select(group => group.First().Span).ToList();
            var finding = new Overloaded(sites[0], pattern.ToString(), sites.Count);

            foreach (var site in sites.Skip(1)) finding.Alongside(site, "also declared here");

            found.Add(finding);
        }

        return found;
    }

    /// <summary>
    ///     A declaration's parameter types as a key over their SORTS, not their
    ///     spellings.
    /// </summary>
    ///
    /// <remarks>
    ///     By sort so that two spellings of one type are one signature: «number»
    ///     and «(number)» resolve to the same sort, and under equality unification
    ///     they are the same declaration written twice — a duplicate that must
    ///     survive, not an overload waiting to expire into type-directed selection
    ///     (REAUDIT54 finding 3). Keying by spelling filed such a pair as an overload,
    ///     so the ledgered expiry would one day make a genuine duplicate legal.
    ///     <para>
    ///     NAMES are absent on purpose — «area of (radius => number)» and «area of
    ///     (r => number)» are one declaration, and what a parameter is called is the
    ///     callee's business. Each block keeps its arity, because «(a, b) with (c)»
    ///     and «(a) with (b, c)» distribute their sorts differently and a caller
    ///     brackets each its own way. A parameter with no annotation, or one whose
    ///     words are not a type, falls back to its spelling — the classifier has
    ///     nothing better to say, and an unknown type is a finding of its own.
    ///     </para>
    ///     <para>
    ///     Reads the sorts already resolved onto the signature (<see cref="Resolved"/>)
    ///     rather than resolving again — the store beside the spelling is what a later
    ///     checker reads too (REAUDIT55 finding 3).
    ///     </para>
    /// </remarks>
    private static IReadOnlyList<object> Sorted(Signature signature)
    {
        List<object> key = [];

        for (var block = 0; block < signature.Types.Count; block++)
        {
            key.Add(signature.Types[block].Count);

            for (var slot = 0; slot < signature.Types[block].Count; slot++)
                key.Add(signature.ParameterSorts[block][slot] ?? (object)signature.Types[block][slot]);
        }

        return key;
    }

    /// <summary>The same signature with the sort each of its spellings resolves to, against THIS table.</summary>
    internal Signature Resolved(Signature signature)
    {
        Resolver resolver = new(Symbols, kind: SymbolKind.Type);

        IReadOnlyList<IReadOnlyList<Sort>> parameters =
            [.. signature.Types.Select(block => (IReadOnlyList<Sort>)[.. block.Select(type => SortOf(type, resolver))])];

        return signature with { ParameterSorts = parameters, ReturnSort = SortOf(signature.Return, resolver) };
    }

    /// <summary>The sort a type spelling resolves to here, or null where the words are no one type.</summary>
    private Sort SortOf(string spelling, Resolver resolver)
        => spelling is not null && resolver.Resolve(spelling).TryTree(out var tree) ? Sort.Of(tree, ContainerOf) : null;

    /// <summary>A function's return spelling, or null where it declares no return type.</summary>
    private static string Returned(Function function)
        => function.Returns is Type.Unresolved { Reference: { } reference }
         ? string.Join(' ', reference.ToLexemes().Select(lexeme => lexeme.Text))
         : null;

    /// <summary>Two parameter-sort keys equal element by element — arities, sorts, and spellings alike.</summary>
    private sealed class Keying : IEqualityComparer<IReadOnlyList<object>>
    {
        public static Keying Comparer { get; } = new();

        public bool Equals(IReadOnlyList<object> left, IReadOnlyList<object> right) => left.SequenceEqual(right);

        public int GetHashCode(IReadOnlyList<object> key)
        {
            System.HashCode hash = new();

            foreach (var part in key) hash.Add(part);

            return hash.ToHashCode();
        }
    }

    /// <summary>Declares a loop's variable into the body it is bound in.</summary>
    ///
    /// <remarks>
    ///     Through the same refusal a written declaration goes through, because
    ///     it collides the same way: an outer «bank» and a loop over «bank» is
    ///     shadowing, which is a finding, and not a redeclaration, which throws.
    /// </remarks>
    private void Bind(Identifier variable)
    {
        if (Refuse(variable, Role.Binding)) return;

        var words = variable.Canonical;
        var name = variable.Words;
        var span = variable.Span(source);

        if (Refused(name, span)) return;

        written[name] = span;
        Symbols.WithNames(name);

        symbols.Add(new Declared(name, span) { Words = words });

        // The loop's counter, derived from the variable rather than a bare
        // «index». There is no shadowing in this language, so a bare one would
        // collide with every «var index» anyone writes — and "rename your
        // variable because the loop wanted the word" is the diagnostic the
        // grammar exists to avoid. Derived, it nests for free: «index of bank»
        // and «index of branch» coexist with no rule to say how.
        //
        // Not reactive. «old index of bank» would be the previous iteration's
        // counter, which is the current one minus one, and a synonym that looks
        // like it means something is what «old pi» is refused for.
        // Through the same refusal, because it is a declaration too. Skipping
        // it meant an existing «index of bank» let the symbol set silently
        // absorb the duplicate while the diagnostic metadata took a second
        // entry — and the rules key names into a dictionary, so the compiler
        // died on the collision it was meant to report. Declaring the same name
        // INSIDE the loop was refused correctly the whole time; declaring it
        // first killed the process.
        var counter = Injection.Counter.Of(name);

        if (Refused(counter, span)) return;

        written[counter] = span;
        Symbols.WithNames(counter);

        symbols.Add(new Declared(counter, span, InjectedBy: name) { Words = Injection.Counter.Of(words) });
    }

    /// <summary>Declares a parameter into the body it is bound in.</summary>
    ///
    /// <remarks>
    ///     Through the same refusal as everything else, which is the point: a
    ///     parameter's identifier used to reach only <c>Named</c>, which takes
    ///     its rendering — so writability, the reserved prefix, collisions, R5
    ///     and no-shadowing were all asked of nothing. Two parameters whose
    ///     canonical words differ but whose renderings agree became one runtime
    ///     key, and the second argument silently overwrote the first.
    ///     <para>
    ///     No counter, and not reactive. «old (_)» accepts only a reactive
    ///     reference, while a parameter is bound once per call.
    ///     </para>
    /// </remarks>
    private void Receive(Identifier parameter)
    {
        if (Refuse(parameter, Role.Binding)) return;

        var name = parameter.Words;
        var span = parameter.Span(source);

        if (Refused(name, span)) return;

        written[name] = span;
        Symbols.WithNames(name);

        symbols.Add(new Declared(name, span) { Words = parameter.Canonical });
    }



    /// <summary>
    ///     Whether a name cannot be introduced here, having said why.
    /// </summary>
    private bool Refused(string name, Span span)
    {
        // Supplied rather than declared, so «already declared, rename this one»
        // would point at a declaration that does not exist. The pattern case has
        // said this properly since «old (_)» arrived; the name case is the same
        // sentence about the same thing.
        //
        // WHOLE spellings, which is every nullary thing the language supplies —
        // the two truths, and «return» and «stop». A nullary pattern reserved
        // nothing before this: «var return» was declarable and then every bare
        // «return» in scope had two readings, with no bracket able to separate a
        // name from a call over the same span.
        if (SymbolTable.Whole.Contains(name))
        {
            problems.Add(new Supplied(span, name));
            return true;
        }

        if (Symbols.Names.ContainsKey(name) is false) return false;

        var shadowed = new Shadowed(span, name, Where(name));

        // the site being shadowed, which may be in another file entirely
        if (written.TryGetValue(name, out var first)) shadowed.Alongside(first, "first declared here");

        problems.Add(shadowed);
        return true;
    }

    private void Declare(Statement statement)
    {
        // Member.Unresolved is a statement that mentions names rather than
        // declaring any, and it is the shape an ordinary expression takes
        if (statement is not Member member || member.Identifier is null) return;

        // Before anything asks what it declares. An empty bracket makes it a
        // PATTERN — «function ping ()» has a hole, so it took the pattern path
        // and never met a single one of the identifier checks below.
        if (Refuse(member.Identifier, Role.Member)) return;

        // And its parameters, which are declarations too and were checked by
        // nothing at all: they reach «Identifier.Words», which drops every
        // parameter block, so a hole inside one disappeared into a runtime name
        // with no finding. Checked HERE as well as on body entry, because this
        // is what installs the flattened block.
        if (Parameters(member.Identifier).Any(parameter => Refuse(parameter, Role.Binding))) return;

        if (member.Identifier.TryPattern(out var pattern, out var blocks) is false)
        {
            // Too wide is not "not a pattern": it HAS holes, so it is a pattern
            // declaration and one this will not match. Saying so is the whole
            // point of the ceiling — a bound that refuses hostile input by
            // terminating the compiler is not a bound.
            //
            // Asked of a pattern only. The limit exists because matching recurses
            // once per segment, and a plain name never enters that matcher:
            // deciding on width alone told someone with a 129-word name that a
            // pattern may have at most 128, which is true and not about them.
            // Infix. Checked before width, because a leading hole is what it IS
            // and the width is incidental.
            Cell(member);

            // A nullary function is name-shaped, so it is reserved as a value name above —
            // but it is a callable, and a callable files a signature. Routing on whether the
            // name has HOLES — «TryPattern» — is a proxy for «is a function», exact only while
            // every function had a parameter, and «function f ()» being ill-formed guaranteed
            // some would not (NULLARYRULING §2). Route on what the member IS: a
            // «Grammar.Function» files its signature under its zero-hole pattern «[f]», whatever
            // its arity, so a bare «f» reference reads its answer. NOT added to the resolver's
            // patterns — a nullary name in both the name table and the pattern table reads two
            // ways, the very thing the reservation removes — so «f» stays a name and the
            // checker reads «[f]» from the overloads.
            if (member is Grammar.Function nullary && member.Identifier.IsPattern is false)
            {
                var shape = new Compiler.Pattern(member.Identifier.Shaped);

                // Only the FIRST no-argument «f» files a signature. A second is a nullary
                // overload set with no cue at the use site — a bare «f» carries no argument,
                // and the return type is not one — and the name reservation already refuses
                // it: «Cell» shadowed it above, the same family as two same-named types
                // (NULLARYRULING §2). So it is not filed, and one is the whole of the set.
                if (Overloads.ContainsKey(shape) is false)
                {
                    Overloads[shape] = [new Signature(blocks, member.Identifier.Annotations, Returned(nullary),
                                                      Span: member.Identifier.Span(source))];

                    // The callable half: the resolver offers «[f]» in place of the reserved name,
                    // so a bare «f» resolves to a Call every consumer reads as one (NULLARYRULING
                    // §1). Keyed by the name «Cell» reserved and the very «shape» filed above, so
                    // the Call's pattern is the overload key and its answer is read straight back.
                    Symbols.WithNullary(member.Identifier.Words, shape);
                }
            }

            return;
        }

        // Only a function may be a pattern. A datum or a datatype named with a
        // parameter list — «var provide (x)» — is a name given a callable shape it
        // cannot have (docs/spec §4.5.1), and installing it as a pattern would file a
        // value or a type where a call is looked up; casting it to the function it is
        // not terminated the compiler outright (REAUDIT56 finding 1).
        if (member is not Grammar.Function function)
        {
            problems.Add(new Parameterized(member.Identifier.Span(source), member.Identifier.Shape));
            return;
        }

        if (SymbolTable.Builtins.Contains(pattern))
        {
            problems.Add(new Supplied(member.Identifier.Span(source), pattern.ToString()));
            return;
        }

        // A shape goes into the table ONCE. Two declarations sharing one are two
        // things a call could mean, not two ways to read it — inserting both made
        // R3's tie machinery answer a question it was never asked, so every call
        // to an overloaded shape came back ambiguous.
        if (Symbols.Patterns.Any(entry => entry.Pattern.Equals(pattern)) is false)
            Symbols.Patterns.Add((pattern, SymbolKind.Value));

        if (Overloads.TryGetValue(pattern, out var declared) is false) Overloads[pattern] = declared = [];

        declared.Add(new Signature(blocks, member.Identifier.Annotations, Returned(function),
                                   Span: member.Identifier.Span(source)));

        if (shapes.TryGetValue(pattern, out var spans) is false) shapes[pattern] = spans = [];

        spans.Add(new Shape(pattern, member.Identifier.Span(source)));
    }

    /// <summary>
    ///     A value-holding declaration. Lets and explicitly reactive data are
    ///     recorded as the references the built-in «old (_)» may accept.
    /// </summary>
    private void Cell(Member member)
    {
        // Not checked again here. Every route to this passes the identifier
        // through the refusal first, and a second guard that cannot fire is a
        // guard nothing is testing.
        var words = member.Identifier.Canonical;
        var name = member.Identifier.Words;

        var span = member.Identifier.Span(source);

        if (Refused(name, span)) return;

        written[name] = span;

        if (member is Datum { Mutability: Constant })
        {
            Symbols.Constants(name);
            symbols.Add(new Declared(name, span) { Words = words });
        }
        else if (member is Datum datum)
        {
            // Reactive is a property of the referenced symbol, not of a name
            // generated beside it. The built-in «old (_)» asks this set after
            // its hole has resolved to a bare name; imperative data remains an
            // ordinary name and therefore cannot be passed to it.
            if (datum.Mutability is Let || datum.Modifiers.Is<Reactive>()) Symbols.WithReactives(name);
            else Symbols.WithNames(name);

            symbols.Add(new Declared(name, span) { Words = words });
        }
        else
        {
            // A TYPE is a name like any other, in the same table, and the kind is
            // what tells it from the rest — not a table of its own, which would
            // need the position to choose between them and could not answer
            // «type of x». A declaration is what puts it there; a definition is
            // what gives it structure, and «type x;» with neither is a name you
            // can use and cannot construct, which is what a library handle is.
            Symbols.WithNames(member is Grammar.Type ? SymbolKind.Type : SymbolKind.Value, name);
            symbols.Add(new Declared(name, span, Container: container) { Words = words });
        }
    }

    /// <summary>What a declaration is allowed to be, where it is written.</summary>
    private enum Role
    {
        /// <summary>A member, which may declare a pattern.</summary>
        Member,

        /// <summary>A parameter or a loop variable, which is a name.</summary>
        Binding,
    }

    /// <summary>
    ///     Every rule an identifier has to satisfy to declare anything, in the
    ///     order that names the problem rather than a consequence of it.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE analysis and not three. «Declare», «Bind» and «Receive» each
    ///     called a different subset, so what an identifier was checked for
    ///     depended on where it was written — a parameter got writability and
    ///     collisions and nothing else, which is how «function outer (() =&gt;
    ///     Number)» reached the symbol table as the empty string.
    /// </remarks>
    private bool Refuse(Identifier identifier, Role role)
    {
        var span = identifier.Span(source);

        if (identifier.HasEmptyHole)
        {
            problems.Add(new EmptyHole(span, identifier.Shape));
            return true;
        }

        if (role is Role.Binding && identifier.IsPattern)
        {
            problems.Add(new HoleInName(span, identifier.Shape));
            return true;
        }

        if (role is Role.Member && identifier.BeginsWithHole)
        {
            problems.Add(new LeadingHole(span, identifier.Shape));
            return true;
        }

        // Width BEFORE readback, and not the other way round. A declaration can
        // be both, and the readback the unwritable finding prints used to go
        // through the pattern constructor — which enforces this very bound by
        // throwing, so reporting one problem crashed on the other. The readback
        // no longer constructs, and this order means it is not even reached:
        // too wide is refused on what was written, before anything asks what it
        // would read as.
        if (identifier.IsPattern && identifier.Width > Compiler.Pattern.MaxSegments)
        {
            problems.Add(new PatternTooWide(span, identifier.Words, identifier.Width,
                                            Compiler.Pattern.MaxSegments));
            return true;
        }

        if (identifier.Writable) return false;

        problems.Add(new UnwritableName(span, identifier.Declares(), identifier.Reads()));
        return true;
    }

    /// <summary>The identifiers every parameter block of an identifier declares.</summary>
    private static IEnumerable<Identifier> Parameters(Identifier identifier)
        => identifier.Where(component => component.AsParameters is not null)
                     .SelectMany(component => component.AsParameters)
                     .Select(parameter => parameter.AsDatum.Identifier);

    private string Where(string name) => inherited.Contains(name) ? "in an enclosing scope" : "in this scope";

    /// <summary>
    ///     Every declaration of each shape — what its parameters are called, and
    ///     what they were declared to be.
    /// </summary>
    ///
    /// <remarks>
    ///     ALREADY A SET, and it always was: a shape goes into the symbol table
    ///     once and its declarations accumulate here, which is why the runtime
    ///     can say "ambiguous after type filtering" about a case the declaration
    ///     rule currently refuses outright. What was missing is the types, so a
    ///     narrowing pass had nothing to narrow ON.
    ///     <para>
    ///     This said overload choice was "a phase AFTER resolution", which was
    ///     measured and is wrong: a pattern with several declarations has no one
    ///     parameter type while readings are being eliminated, so a later pass
    ///     cannot narrow on it — and every call to an overloaded shape becomes
    ///     ambiguous between readings only one of which could ever type-check.
    ///     The narrowing belongs in the same pass, on a candidate set carried by
    ///     the derivation, so that emptying the set kills the reading through the
    ///     elimination that already exists.
    ///     </para>
    /// </remarks>
    public Dictionary<Compiler.Pattern, List<Signature>> Overloads { get; } = [];

    /// <summary>
    ///     One declaration of a shape: its parameters' names and their declared
    ///     types, each type's spelling BESIDE the sort it resolves to, and the return
    ///     likewise — what the checker unifies without resolving the words again
    ///     (REAUDIT55 finding 3).
    /// </summary>
    ///
    /// <param name="Types">
    ///     One per parameter, positionally matching <paramref name="Names"/>, and
    ///     null where the parameter was written without one.
    /// </param>
    /// <param name="Return">The return type's spelling, null where none was written.</param>
    /// <param name="ParameterSorts">
    ///     The sort each spelling in <paramref name="Types"/> resolves to, in the same
    ///     shape, null per slot where the words are no one type. Filled once the whole
    ///     table stands, so null until then.
    /// </param>
    /// <param name="ReturnSort">The sort <paramref name="Return"/> resolves to, filled alongside.</param>
    /// <param name="Span">
    ///     The declaration's span, which locates it in the enclosing table where it is
    ///     registered so its owning function can find it and resolve its sorts against
    ///     its own table (REAUDIT56 finding 2).
    /// </param>
    public readonly record struct Signature(
        Blocks Names, Blocks Types, string Return = null,
        IReadOnlyList<IReadOnlyList<Sort>> ParameterSorts = null, Sort ReturnSort = null, Span Span = default);

    private readonly List<Finding> problems = [];
    private readonly Dictionary<string, Span> written = [];
    private readonly Dictionary<Compiler.Pattern, List<Shape>> shapes = [];
    private IReadOnlyList<Finding> found;
    private readonly List<Declared> symbols = [];
    private SourceText source;
    private Container container;
    private readonly HashSet<string> inherited = [];

    /// <summary>
    ///     The nearest named container a type «name» belongs to — its identity under
    ///     SCOPE-IDENTITY-RULING's H, where a type declaration belongs to the module,
    ///     type, or function that contains it, not the block it sits in. Rooted in the
    ///     module, with no segments for a type declared at the module. The path is
    ///     stable because the containers are named; a merged declaration keeps the
    ///     container it was declared in.
    /// </summary>
    internal Container ContainerOf(string name)
        => symbols.First(declared => declared.Name == name).Container;
}
