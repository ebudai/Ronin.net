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
    public IReadOnlyList<Finding> Problems => found ??= [.. problems.Concat(Rules.Validate(symbols, [.. shapes.SelectMany(
        shape => shape.Value.Select(shaped => new Shape(shape.Key, shaped.Span, shaped.Inherited)))]))];

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
    public static Declarations Of(IEnumerable<Statement> statements, SourceText source, Declarations enclosing = null)
    {
        Declarations declarations = new() { source = source };

        if (enclosing is not null)
        {
            declarations.Symbols.Merging(enclosing.Symbols);
            declarations.inherited.UnionWith(enclosing.Symbols.Names);

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

        foreach (var statement in statements) declarations.Declare(statement);

        // After every statement, because a shape is over-declared by its set and
        // not by any one member of it — and the last declaration is as much a
        // participant as the first.
        foreach (var (pattern, spans) in declarations.shapes)
        {
            if (spans.Count < 2) continue;

            var finding = new Overloaded(spans[0].Span, pattern.ToString(), spans.Count);

            foreach (var shaped in spans.Skip(1)) finding.Alongside(shaped.Span, "also declared here");

            declarations.problems.Add(finding);
        }

        return declarations;
    }

    private void Declare(Statement statement)
    {
        // Member.Unresolved is a statement that mentions names rather than
        // declaring any, and it is the shape an ordinary expression takes
        if (statement is not Member member || member.Identifier is null) return;

        if (member.Identifier.TryPattern(out var pattern, out var blocks) is false)
        {
            // Too wide is not "not a pattern": it has holes, so it is a pattern
            // declaration and it is one this will not match. Saying so is the
            // whole point of the ceiling — a bound that refuses hostile input by
            // terminating the compiler is not a bound.
            if (member.Identifier.Width > Compiler.Pattern.MaxSegments)
            {
                problems.Add(new PatternTooWide(member.Identifier.Span(source),
                                                member.Identifier.Words,
                                                member.Identifier.Width,
                                                Compiler.Pattern.MaxSegments));
                return;
            }

            Cell(member);
            return;
        }

        // A shape goes into the table ONCE. Two declarations sharing one are two
        // things a call could mean, not two ways to read it — inserting both made
        // R3's tie machinery answer a question it was never asked, so every call
        // to an overloaded shape came back ambiguous.
        if (Symbols.Patterns.Contains(pattern) is false) Symbols.Patterns.Add(pattern);

        if (Overloads.TryGetValue(pattern, out var declared) is false) Overloads[pattern] = declared = [];

        declared.Add(blocks);

        if (shapes.TryGetValue(pattern, out var spans) is false) shapes[pattern] = spans = [];

        spans.Add(new Shape(pattern, member.Identifier.Span(source)));
    }

    /// <summary>
    ///     A value-holding declaration. Only these inject a shadow, and a constant
    ///     does not even do that — its previous value is provably its current one.
    /// </summary>
    private void Cell(Member member)
    {
        var name = member.Identifier.Words;

        var span = member.Identifier.Span(source);

        // No related span: a reserved word has no prior declaration to point at,
        // and pointing anywhere would be inventing one.
        if (name.StartsWith(SymbolTable.Shadowed, System.StringComparison.Ordinal))
        {
            problems.Add(new ReservedPrefix(span, name, SymbolTable.Old));
            return;
        }

        if (Symbols.Names.Contains(name))
        {
            var shadowed = new Shadowed(span, name, Where(name));

            // the site being shadowed, which may be in another file entirely
            if (written.TryGetValue(name, out var first)) shadowed.Alongside(first, "first declared here");

            problems.Add(shadowed);
            return;
        }

        written[name] = span;

        if (member is Datum { Mutability: Constant })
        {
            Symbols.Constants(name);
            symbols.Add(new Declared(name, span));
        }
        else if (member is Datum)
        {
            Symbols.Declaring(name);

            // the shadow carries its origin's span, because it has none of its
            // own and is not the programmer's to rename
            symbols.Add(new Declared(name, span));
            symbols.Add(new Declared(SymbolTable.Shadowed + name, span, InjectedBy: name));
        }
        else
        {
            Symbols.WithNames(name);
            symbols.Add(new Declared(name, span));
        }
    }

    private string Where(string name) => inherited.Contains(name) ? "in an enclosing scope" : "in this scope";

    /// <summary>
    ///     Every declaration of each shape, each as its parameter names by hole.
    /// </summary>
    ///
    /// <remarks>
    ///     A list because overloads share a shape and are separated later by type.
    ///     Overload choice is a phase after resolution — enumerate, type-filter,
    ///     rank by lookup, tie is an error — and not something the resolver can
    ///     see, which is why the shape reaches it only once.
    /// </remarks>
    public Dictionary<Compiler.Pattern, List<Blocks>> Overloads { get; } = [];

    private readonly List<Finding> problems = [];
    private readonly Dictionary<string, Span> written = [];
    private readonly Dictionary<Compiler.Pattern, List<Shape>> shapes = [];
    private IReadOnlyList<Finding> found;
    private readonly List<Declared> symbols = [];
    private SourceText source;
    private readonly HashSet<string> inherited = [];
}
