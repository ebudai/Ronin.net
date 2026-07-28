// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ronin.Compiler;

/// <summary>
///     Resolves a statement to a unique meaning by minimum lookup count.
/// </summary>
///
/// <remarks>
///     <para>
///     Replaces <c>Identifier.Resolve</c>. That method received a single greedy
///     <c>Name</c> spanning every consecutive word, and tried to recover the real
///     boundaries afterwards by enumerating index permutations. This never
///     commits to a boundary at all: a phrase is a SPAN, every span is scored,
///     and the cheapest scoring wins.
///     </para>
///     <para>
///     <c>E[i, j, m]</c> is the cheapest expression over tokens <c>i..j-1</c>
///     parsed at minimum binding power <c>m</c>. The third index is what makes
///     precedence work: a pattern call ending in an unbracketed trailing
///     argument returns at level <see cref="PatternBindingPower"/>, so it may
///     only be an operand where <c>m &lt;= PatternBindingPower</c>. That is
///     precedence climbing expressed as a table constraint rather than as
///     control flow.
///     </para>
///     <para>
///     Cost is the number of symbol table lookups: one per name reference, one
///     per pattern call, one per bracketed substatement regardless of its size.
///     Literals and operators are free because neither consults the table.
///     Equal-cost readings are a tie, and a tie is an error — never a silent
///     pick. The repair is to bracket, which promotes an argument to its own
///     substatement.
///     </para>
/// </remarks>
internal sealed class Resolver
{
    /// <summary>Highest binding power the table indexes. Must exceed every operator.</summary>
    public const int MaxBindingPower = 30;

    /// <summary>
    ///     The longest statement that will be resolved. Far past anything anyone
    ///     writes on one line, and short of what a cubic table makes expensive.
    /// </summary>
    public const int MaxLexemes = 256;

    public Resolver(SymbolTable symbols, int patternBindingPower = 7)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        if (patternBindingPower < 0 || patternBindingPower > MaxBindingPower)
            throw new ArgumentOutOfRangeException(nameof(patternBindingPower));

        this.symbols = symbols;
        PatternBindingPower = patternBindingPower;

        // The minimum binding powers the recurrences can ever ASK for, which is
        // a handful and not a range. A table indexed 0..31 spent five sixths of
        // itself on levels nothing queries — «E[i, j, 13]» is reachable only if
        // some operator binds at 13, and none does.
        //
        // Derived from the operator table rather than written down: an operator
        // added at a new level would otherwise index a slot that does not exist,
        // and the statements using it would silently fail to resolve.
        SortedSet<int> reachable = [0, PatternBindingPower];

        foreach (var op in symbols.Operators.Values)
        {
            // the side that may repeat the operator takes its own power, the
            // side that may not takes one more
            reachable.Add(op.BindingPower);
            reachable.Add(op.BindingPower + 1);
        }

        minima = [.. reachable];
        slots = new int[MaxBindingPower + 2];

        for (var slot = 0; slot < minima.Length; ++slot) slots[minima[slot]] = slot;
    }

    /// <summary>
    ///     Where a word pattern binds relative to the operators. A trailing
    ///     argument absorbs operators binding at least this tightly and stops at
    ///     looser ones. Loose enough to sit under arithmetic, tight enough to sit
    ///     above the plumbing operators.
    /// </summary>
    public int PatternBindingPower { get; }

    /// <remarks>Not thread safe: the tables are instance state, reallocated per call.</remarks>
    public Resolution Resolve(IReadOnlyList<Lexeme> lexemes)
    {
        ArgumentNullException.ThrowIfNull(lexemes);
        if (lexemes.Count is 0) return Resolution.NoParse;

        // The table is cubic in the lexeme count, so one generated or pasted
        // statement can ask for arbitrarily much of it. Per-statement resolution
        // bounds the ordinary case and not that one, and a compiler that stops
        // is better than a compiler that is stopped.
        if (lexemes.Count > MaxLexemes) return Resolution.TooLong;

        var n = lexemes.Count;

        // Patterns by their first word, rebuilt per call because a scope may have
        // gained one since the last.
        anchored = [];
        foreach (var pattern in symbols.Patterns)
        {
            var first = pattern.Segments[0];

            if (anchored.TryGetValue(first, out var sharing) is false) anchored[first] = sharing = [];

            sharing.Add(pattern);
        }

        // Triangular. A span runs from «i» to «j» with i <= j, so half of a
        // rectangular table is spans that do not exist and were allocated anyway
        // — and the largest of the three tables paid for that half once per
        // binding power.
        rows = new int[n + 2];
        for (var i = 0; i <= n; ++i) rows[i + 1] = rows[i] + (n + 1 - i);

        var spans = rows[n + 1];

        closed = NewTable(spans);
        open = NewTable(spans);

        expressions = new Cell[spans * minima.Length];
        for (var cell = 0; cell < expressions.Length; ++cell) expressions[cell] = new Cell();

        for (var width = 1; width <= n; ++width)
        {
            for (var i = 0; i + width <= n; ++i)
            {
                var j = i + width;
                Atoms(lexemes, i, j);

                // descending so that a narrower minimum is filled after the
                // wider ones it may draw on at the same span
                for (var slot = minima.Length - 1; slot >= 0; --slot) Expression(lexemes, i, j, minima[slot]);
            }
        }

        var top = Expressions(0, n, 0);
        if (top.TryBest(out var best) is false) return Resolution.NoParse;

        return best.Count > 1 ? Resolution.Ambiguous(best.Cost, top.Readings)
                              : Resolution.Resolved(best.Cost, best.Node);
    }

    /// <summary>
    ///     Lexes and resolves in one step, through the real lexer.
    /// </summary>
    ///
    /// <remarks>
    ///     This used to call a splitter written beside <see cref="Lexeme"/> for
    ///     the tests, which made it a second lexer — and it diverged, most
    ///     sharply on symbols: it munched «&lt;=» into one lexeme where
    ///     <c>Symbol.Lex</c> advances a single character, which is the very
    ///     divergence the operator table warns about. Every resolver test went
    ///     through it, so twenty-three expectations said something about the
    ///     splitter and only something about the compiler by agreement.
    /// </remarks>
    public Resolution Resolve(string source) => Resolve(Lexemes.Lex(source));

    private static Cell[] NewTable(int spans)
    {
        var table = new Cell[spans];
        for (var span = 0; span < spans; ++span) table[span] = new Cell();
        return table;
    }

    /// <summary>Where the span «i..j» sits, counting only spans that exist.</summary>
    private int Span(int i, int j) => rows[i] + (j - i);

    /// <summary>
    ///     Fills the atom tables for one span. <see cref="closed"/> holds atoms
    ///     complete in themselves; <see cref="open"/> holds pattern calls ending
    ///     in an unbracketed trailing argument, which are the ones precedence has
    ///     to constrain.
    /// </summary>
    private void Atoms(IReadOnlyList<Lexeme> lexemes, int i, int j)
    {
        var cell = closed[Span(i, j)];

        if (j - i is 1 && lexemes[i].Kind is LexemeKind.Number) cell.Offer(0, new Node.Literal(lexemes[i].Text));

        if (AllWords(lexemes, i, j))
        {
            var name = string.Join(' ', Enumerable.Range(i, j - i).Select(k => lexemes[k].Text));
            if (symbols.Names.Contains(name)) cell.Offer(1, new Node.Name(name));
        }

        // a bracketed substatement is one lookup however large it is, and it is
        // CLOSED, which is what lets «(compute total for a) + b» resolve
        if (j - i >= 2 && lexemes[i].Kind is LexemeKind.Open && lexemes[j - 1].Kind is LexemeKind.Close)
        {
            Group(lexemes, cell, i + 1, j - 1);
        }

        // Only the patterns whose first word is the one actually sitting at «i».
        // Every pattern begins with a word — a leading hole is left recursive and
        // rejected at construction — so a pattern that starts with anything else
        // cannot match this span, and asking it was a table walk per pattern per
        // span to arrive at the same answer.
        if (lexemes[i].Kind is not LexemeKind.Word) return;
        if (anchored.TryGetValue(lexemes[i].Text, out var candidates) is false) return;

        foreach (var pattern in candidates)
        {
            var target = pattern.IsOpenEnded ? open[Span(i, j)] : cell;
            foreach (var (cost, arguments, count) in Match(pattern, 0, lexemes, i, j))
                target.Offer(1 + cost, new Node.Call(pattern, arguments), count);
        }
    }

    /// <summary>
    ///     Offers the contents of a bracketed span as one substatement, split on
    ///     the separators at its own depth.
    /// </summary>
    ///
    /// <remarks>
    ///     «(x, y)» is a group of two where «(x)» is a group of one, and the two
    ///     are the same shape — which is what lets a parameter block of two bind
    ///     to a hole while the resolver stays ignorant of arity. It costs one
    ///     lookup either way, because it is one bracketed substatement.
    /// </remarks>
    private void Group(IReadOnlyList<Lexeme> lexemes, Cell cell, int from, int to)
    {
        List<int> separators = [];
        var depth = 0;

        for (var k = from; k < to; ++k)
        {
            switch (lexemes[k].Kind)
            {
                case LexemeKind.Open: ++depth; break;
                case LexemeKind.Close: --depth; break;
                case LexemeKind.Separator when depth is 0: separators.Add(k); break;
                default: break;
            }
        }

        var cost = 0;
        var count = 1L;
        List<Node> parts = [];
        var start = from;

        foreach (var end in separators.Append(to))
        {
            // an empty part — «(a,)» or «()» — is not a substatement at all
            if (Expressions(start, end, 0).TryBest(out var part) is false) return;

            cost += part.Cost;

            // Saturate every step, as Match and Expression already do. Saturating
            // only at the end left a raw product across the parts, and a group of
            // 63 independently ambiguous parts reached 2^63 — which wraps to
            // negative, is duly reported as fewer than two derivations, and
            // returns a genuine tie as Resolved.
            count = Cell.Saturating(count * part.Count);

            parts.Add(part.Node);
            start = end + 1;
        }

        cell.Offer(1 + cost, new Node.Group(parts), count);
    }

    private static bool AllWords(IReadOnlyList<Lexeme> lexemes, int i, int j)
    {
        for (var k = i; k < j; ++k) if (lexemes[k].Kind is not LexemeKind.Word) return false;
        return true;
    }

    /// <summary>
    ///     Every way <paramref name="pattern"/> can cover the span, given as the
    ///     arguments filling its holes left to right. A literal segment has to
    ///     match but contributes no argument, which is why the words do not appear
    ///     here at all — <see cref="Node.Call"/> puts them back by walking the
    ///     same segments when it renders.
    /// </summary>
    private IEnumerable<(int Cost, IReadOnlyList<Node> Arguments, long Count)> Match(
        Pattern pattern, int segment, IReadOnlyList<Lexeme> lexemes, int position, int end)
    {
        if (segment == pattern.Segments.Count)
        {
            if (position == end) yield return (0, [], 1);
            yield break;
        }

        var word = pattern.Segments[segment];
        if (word is not null)
        {
            if (position < end && lexemes[position].Kind is LexemeKind.Word && lexemes[position].Text == word)
                foreach (var match in Match(pattern, segment + 1, lexemes, position + 1, end))
                    yield return match;
            yield break;
        }

        if (segment == pattern.Segments.Count - 1)
        {
            // trailing argument: reaches the end of the span, parsed at the
            // pattern's own binding power
            if (Expressions(position, end, PatternBindingPower).TryBest(out var trailing))
                yield return (trailing.Cost, [trailing.Node], trailing.Count);
            yield break;
        }

        for (var split = position + 1; split <= end; ++split)
        {
            // medial args cross any operator
            if (Expressions(position, split, 0).TryBest(out var argument) is false) continue;
            foreach (var (cost, arguments, count) in Match(pattern, segment + 1, lexemes, split, end))
                yield return (argument.Cost + cost, [argument.Node, .. arguments], Cell.Saturating(argument.Count * count));
        }
    }

    private void Expression(IReadOnlyList<Lexeme> lexemes, int i, int j, int minimum)
    {
        var cell = Expressions(i, j, minimum);
        cell.Merge(closed[Span(i, j)]);

        // an open pattern call returns at PatternBindingPower, so it is only
        // available where no tighter minimum is demanded
        if (minimum <= PatternBindingPower) cell.Merge(open[Span(i, j)]);

        var depth = 0;
        for (var k = i; k < j; ++k)
        {
            switch (lexemes[k].Kind)
            {
                case LexemeKind.Open: ++depth; continue;
                case LexemeKind.Close: --depth; continue;
                case LexemeKind.Symbol when depth is 0 && k > i && k < j - 1: break;
                default: continue;
            }

            if (symbols.Operators.TryGetValue(lexemes[k].Text, out var op) is false) continue;
            if (op.BindingPower < minimum) continue;

            // An operator is admitted where its binding power is at least the
            // minimum, so the side that may repeat it takes the operator's own
            // power and the side that may not takes one more. Left associative
            // therefore groups «a + b + c» as «(a + b) + c», and right
            // associative mirrors it. Handing both sides the higher minimum
            // forbids the operator on either, and a chain of one precedence
            // stops parsing altogether.
            var repeats = op.BindingPower;
            var excludes = op.BindingPower + 1;
            var leftminimum = op.IsLeftAssociative ? repeats : excludes;
            var rightminimum = op.IsLeftAssociative ? excludes : repeats;

            if (Expressions(i, k, leftminimum).TryBest(out var left) is false) continue;
            if (Expressions(k + 1, j, rightminimum).TryBest(out var right) is false) continue;

            cell.Offer(left.Cost + right.Cost,
                       new Node.Operation(left.Node, lexemes[k].Text, op, right.Node),
                       Cell.Saturating(left.Count * right.Count));
        }
    }

    /// <summary>
    ///     The cell for a span at a minimum binding power. Only the minima the
    ///     recurrences can ask for have a slot, so this maps one to the other.
    /// </summary>
    private Cell Expressions(int i, int j, int minimum) => expressions[(Span(i, j) * minima.Length) + slots[minimum]];

    private Dictionary<string, List<Pattern>> anchored;
    private readonly int[] minima;
    private readonly int[] slots;
    private readonly SymbolTable symbols;
    private int[] rows;
    private Cell[] closed;
    private Cell[] open;
    private Cell[] expressions;

    /// <summary>
    ///     Cheapest cost for a span, and how many derivations achieve it. The
    ///     count has to propagate through <see cref="Merge"/> or a tie inside a
    ///     subspan disappears the moment a larger span uses it.
    /// </summary>
    private sealed class Cell
    {
        public int Cost { get; private set; } = int.MaxValue;

        public bool IsEmpty => order is null;

        /// <summary>
        ///     How many derivations reach the cheapest cost, saturating at two.
        /// </summary>
        ///
        /// <remarks>
        ///     The only question ever asked of this is unique-versus-ambiguous, so
        ///     counting past two buys nothing and costs correctness: unbounded
        ///     multiplication across spans can wrap, and a genuinely ambiguous
        ///     parse that wraps to one is reported as resolved.
        /// </remarks>
        public long Count => Saturating(derivations.Values.Sum());

        public IEnumerable<string> Readings => order.Select(node => node.ToString());

        /// <summary>
        ///     The cheapest reading, when the span has one. Every caller of this
        ///     used to read <c>Cost</c>, <c>Reading</c> and <c>Count</c> off the
        ///     cell behind its own <see cref="IsEmpty"/> check, so the empty case
        ///     was tested twice and <c>Reading</c> carried a fallback that could
        ///     not be reached. A span with no parse now has no <see cref="Best"/>
        ///     to hand out, and the caller has to say what it does about that.
        /// </summary>
        public bool TryBest(out Best best)
        {
            if (IsEmpty)
            {
                best = default;
                return false;
            }

            best = new Best(Cost, order[0], Count);
            return true;
        }

        // Keyed by rendering rather than by node: two derivations that read the
        // same way ARE the same reading, and counting them separately would
        // report a tie between a statement and itself.
        /// <summary>Two is as many as anything needs to be counted.</summary>
        public static long Saturating(long count) => count < 2 ? count : 2;

        public void Offer(int cost, Node node, long count = 1)
        {
            var reading = node.ToString();

            if (IsEmpty || cost < Cost)
            {
                Cost = cost;

                // On first offer, not at construction. Most cells in the table
                // are never offered anything — a span that is not an expression
                // still gets one per binding power — so eagerly allocating both
                // collections was two objects per cell for nothing.
                order ??= [];
                derivations ??= [];

                order.Clear();
                derivations.Clear();
                order.Add(node);
                derivations[reading] = count;
                return;
            }
            if (cost != Cost) return;
            if (derivations.ContainsKey(reading) is false) order.Add(node);
            derivations[reading] = Saturating(derivations.GetValueOrDefault(reading) + count);
        }

        public void Merge(Cell other)
        {
            if (other.IsEmpty) return;
            foreach (var node in other.order) Offer(other.Cost, node, other.derivations[node.ToString()]);
        }

        // Dictionary is NOT insertion ordered in .NET, and the chosen reading must
        // be deterministic, so order is tracked explicitly alongside the counts.
        // Both are null until something is offered, which is what «IsEmpty» reads.
        private List<Node> order;
        private Dictionary<string, long> derivations;
    }
}

/// <summary>The cheapest reading of one span, and how many derivations reach it.</summary>
internal readonly record struct Best(int Cost, Node Node, long Count);

internal enum LexemeKind { Word, Number, Symbol, Open, Close, Separator }

internal readonly record struct Lexeme(LexemeKind Kind, string Text)
{
}

/// <summary>A word pattern. A null segment is a hole.</summary>
///
/// <remarks>
///     Identity is the segment sequence, not the rendering. Keying a scope on
///     <see cref="ToString"/> would work until someone wanted a prettier display
///     form, and the failure mode of that divergence is silent scope collisions.
/// </remarks>
internal sealed class Pattern : IEquatable<Pattern>
{
    public Pattern(IReadOnlyList<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count is 0) throw new ArgumentException("pattern is empty", nameof(segments));

        // A pattern beginning with a hole is left recursive: resolving an atom at
        // position p would require resolving an atom at position p. Infix must be
        // symbolic; word patterns must be prefix.
        if (segments[0] is null)
            throw new ArgumentException("a word pattern must begin with a word, not a hole", nameof(segments));

        // Matching recurses one frame per segment, so this is what bounds that
        // recursion. A declaration this wide is not a pattern anyone is reading.
        if (segments.Count > MaxSegments)
            throw new ArgumentException($"a word pattern may have at most {MaxSegments} words and holes",
                                        nameof(segments));

        // Copied, because identity IS the segment sequence and a scope is keyed
        // on it. Keeping the caller's list meant mutating that list changed the
        // hash of a live key: the entry became unreachable both by the pattern
        // that made it and by a freshly built equal one, so a declaration simply
        // vanished from the scope with nothing to show it had.
        Segments = [.. segments];
    }

    /// <summary>The most words and holes one pattern may have.</summary>
    public const int MaxSegments = 128;

    /// <summary>Parses "compute total for _" into segments, "_" being a hole.</summary>
    public static Pattern Parse(string pattern)
        => new([
            .. pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s is "_" ? null : s)
        ]);

    public IReadOnlyList<string> Segments { get; }

    /// <summary>True when the last segment is a hole, so the call has an unbracketed trailing argument.</summary>
    public bool IsOpenEnded => Segments[^1] is null;

    /// <summary>The literal words before the first hole. Anchor runs must be prefix free across a scope.</summary>
    public IReadOnlyList<string> Anchor => [.. Segments.TakeWhile(s => s is not null)];

    /// <summary>Literal segments after the first hole. These are reserved against multi-word names.</summary>
    public IEnumerable<string> Glue => Segments.Skip(Anchor.Count).Where(s => s is not null);

    public override string ToString() => string.Join(' ', Segments.Select(s => s ?? "(_)"));

    public bool Equals(Pattern other) => other is not null && Segments.SequenceEqual(other.Segments);

    public override bool Equals(object obj) => Equals(obj as Pattern);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (var segment in Segments) hash.Add(segment);
        return hash.ToHashCode();
    }
}

/// <summary>
///     One operator: where it binds, which way it groups, and what it does.
/// </summary>
///
/// <remarks>
///     Precedence and meaning together and not in two tables that have to agree.
///     <see cref="Apply"/> is required, so an operator cannot be given a binding
///     power without also being given a meaning — which is the failure the old
///     key-set test could only report after the fact.
///
///     A class and not a record: what matters is which operator object resolution
///     chose, since that is the one evaluation applies, and identity says that
///     where value equality would not.
/// </remarks>
internal sealed class Operator
{
    public Operator(int bindingPower, Func<object, object, object> apply, bool isLeftAssociative = true)
    {
        // Checked here rather than assumed. The table is mutable so that a scope
        // can add an operator, and every one of these failed far from the
        // insertion that caused it: a binding power outside the indexed range
        // came back as a raw IndexOutOfRangeException while CONSTRUCTING a
        // resolver, and a null implementation resolved perfectly well and then
        // threw inside the evaluator. The comment already said an implementation
        // was required; now something says so.
        if (bindingPower < 0 || bindingPower > Resolver.MaxBindingPower)
            throw new ArgumentOutOfRangeException(nameof(bindingPower), bindingPower,
                                                  $"a binding power runs from 0 to {Resolver.MaxBindingPower}");

        ArgumentNullException.ThrowIfNull(apply);

        BindingPower = bindingPower;
        Apply = apply;
        IsLeftAssociative = isLeftAssociative;
    }

    public int BindingPower { get; }

    /// <summary>What the operator does. Required, so resolution and evaluation cannot disagree about whether it has a meaning.</summary>
    public Func<object, object, object> Apply { get; }

    public bool IsLeftAssociative { get; }
}

/// <summary>Names and patterns in scope, plus the fixed operator table.</summary>
internal sealed class SymbolTable
{
    public HashSet<string> Names { get; } = [];

    public List<Pattern> Patterns { get; } = [];

    /// <summary>
    ///     Patterns the grammar provides, in every scope, always.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Here for their GLUE. «for each (_) in (_)» is why «in» may not appear
    ///     in a multi-word name, and that reservation is what makes a loop header
    ///     have exactly one «in» and therefore exactly one reading — see
    ///     LOOPSYNTAX.md, which shows the alternative is not a tie but a
    ///     strictly-cheaper wrong reading that nothing flags.
    ///     </para>
    ///     <para>
    ///     Not in <see cref="Patterns"/>, because today the loop is a grammar
    ///     production and the resolver never sees a loop header. The reserved
    ///     glue set is therefore larger than the pattern table, and will stop
    ///     being so when the resolver takes the loop over.
    ///     </para>
    ///     <para>
    ///     Spelled in the LEXER's words and not the reader's: «for each» is one
    ///     token, as «part of» is, so it is one segment. A pattern is matched
    ///     against lexemes, so its segments have to be things the lexer can
    ///     produce — «for» and «each» as two segments would never match anything.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<Pattern> Builtins { get; } = [new Pattern(["for each", null, "in", null])];

    /// <summary>
    ///     Fixed at language design time. No user defined operators, which is
    ///     what keeps symbol lexing context free and disarms the maximal munch
    ///     trap that «a&lt;-b» represents in Haskell.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Seeded from <see cref="Runtime.Builtin.Operators"/>, which is the one
    ///     table. A copy rather than the table itself because a scope may add to
    ///     it — the resolver's own tests do — and adding to the language's
    ///     definition from a scope would be a different thing entirely.
    ///     </para>
    ///     <para>
    ///     Every entry must be one the lexer can actually produce. <c>Symbol.Lex</c>
    ///     advances a single character, so anything longer needs a
    ///     <c>Symbol.Special</c> of its own — otherwise the entry is dead and the
    ///     statements using it silently fail to resolve.
    ///     </para>
    /// </remarks>
    public Dictionary<string, Operator> Operators { get; } = new(Runtime.Builtin.Operators);

    /// <summary>
    ///     Folds an enclosing scope in, so an inner scope is a flat table rather
    ///     than a chain to walk.
    /// </summary>
    ///
    /// <remarks>
    ///     This is what banning shadowing buys. The resolver does a lookup per
    ///     position per span, and a merged table keeps each one a single probe
    ///     instead of a walk up N levels — so the table is built once on entering
    ///     a scope and the DP never learns that nesting exists.
    /// </remarks>
    public SymbolTable Merging(SymbolTable enclosing)
    {
        foreach (var name in enclosing.Names) Names.Add(name);
        foreach (var name in enclosing.constants) constants.Add(name);

        Patterns.AddRange(enclosing.Patterns);

        return this;
    }

    /// <summary>The scope as it is, without the injection a declaration performs.</summary>
    public SymbolTable WithNames(params string[] names)
    {
        foreach (var name in names) Names.Add(name);
        return this;
    }

    /// <summary>
    ///     Declares cells, each of which injects its shadow into the same scope.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     «old x» is an injected NAME rather than an operator or a pattern, and
    ///     that is what makes it cost nothing: a name is already an atom and an
    ///     atom is already an operand at every binding level. Spelling it as a
    ///     word pattern would make «old» swallow the rest of the expression, and
    ///     spelling it as a prefix operator would need a new atom kind, a binding
    ///     power, and an exemption so that reading «old x» does not put an edge on
    ///     «x». Injection gets that last one free, because «old x» IS a different
    ///     cell — «let smoothed = old smoothed * 0.9 + reading * 0.1» is not a
    ///     self-cycle by construction rather than by exemption.
    ///     </para>
    ///     <para>
    ///     Injection is unconditional and allocation is not. Whether anything
    ///     reads «old x» is unknown until resolution has finished, but the name
    ///     has to be in scope during it, so the symbol always appears and
    ///     <c>Graph.Shadow</c> allocates only where a reference was found.
    ///     </para>
    /// </remarks>
    public SymbolTable Declaring(params string[] names)
    {
        foreach (var name in names)
        {
            if (name.StartsWith(Shadowed, StringComparison.Ordinal))
                throw new ArgumentException(
                    $"«{name}» begins with the reserved word «{Old}». There is no «old old x»: " +
                    "injection applies to declared cells and never to injected ones, so a second " +
                    "generation has to be captured by declaring a let for it.", nameof(names));

            var shadow = Shadowed + name;

            if (Names.Contains(shadow))
                throw new ArgumentException(
                    $"«{shadow}» is already in scope, and declaring «{name}» injects it. " +
                    "Rename whichever of the two you own.", nameof(names));

            Names.Add(name);
            Names.Add(shadow);
        }

        return this;
    }

    /// <summary>
    ///     Declares constants, which are named but get no shadow.
    /// </summary>
    ///
    /// <remarks>
    ///     «old x» is the previous generation's value, and for a constant that is
    ///     provably the current one — so «old pi» would not merely be useless, it
    ///     would be a synonym that looks like it means something. Leaving it
    ///     unresolved lets <see cref="Explain"/> say why instead.
    /// </remarks>
    public SymbolTable Constants(params string[] names)
    {
        foreach (var name in names)
        {
            Names.Add(name);
            constants.Add(name);
        }

        return this;
    }

    /// <summary>
    ///     Why a name that looks like it should be in scope is not, when there is
    ///     something better to say than that it is missing.
    /// </summary>
    public string Explain(string name)
    {
        if (name.StartsWith(Shadowed, System.StringComparison.Ordinal) is false) return null;

        var cell = name[Shadowed.Length..];
        if (constants.Contains(cell) is false) return null;

        return $"no name «{name}» in scope. «{cell}» is a constant, so it has no previous " +
               $"value — use «{cell}».";
    }

    private readonly HashSet<string> constants = [];

    /// <summary>The prefix a declaration injects, and the reserved word it is built from.</summary>
    internal const string Old = "old";
    internal const string Shadowed = Old + " ";

    public SymbolTable WithPatterns(params string[] patterns)
    {
        foreach (var pattern in patterns) Patterns.Add(Pattern.Parse(pattern));
        return this;
    }

}

internal readonly record struct Resolution(ResolutionKind Kind, int Cost, IReadOnlyCollection<string> Readings)
{
    public static readonly Resolution NoParse = new(ResolutionKind.NoParse, 0, []);

    /// <summary>
    ///     Past what will be resolved at once. Distinct from a failure to parse,
    ///     because the statement may well be perfectly good and nothing here
    ///     found out.
    /// </summary>
    public static readonly Resolution TooLong = new(ResolutionKind.TooLong, 0, []);

    public static Resolution Resolved(int cost, Node tree)
        => new(ResolutionKind.Resolved, cost, [tree.ToString()]) { Tree = tree };

    public static Resolution Ambiguous(int cost, IEnumerable<string> readings)
        => new(ResolutionKind.Ambiguous, cost, [.. readings]);

    public string Reading => Readings.FirstOrDefault() ?? string.Empty;

    /// <summary>
    ///     The tree to evaluate, when the statement resolved to exactly one. A tie
    ///     has several and no grounds to choose between them; a statement that did
    ///     not parse has none. Neither hands one out, so an interpreter cannot walk
    ///     a meaning the resolver never settled on.
    /// </summary>
    public bool TryTree(out Node tree)
    {
        tree = Tree;
        return Kind is ResolutionKind.Resolved;
    }

    private Node Tree { get; init; }

    public override string ToString() => Kind switch
    {
        ResolutionKind.NoParse => "no parse",
        ResolutionKind.TooLong => $"more than {Resolver.MaxLexemes} words and symbols in one statement, " +
                                  "which is past what is read at once — split it",
        ResolutionKind.Resolved => $"{Cost} lookup(s): {Reading}",
        _ => Ambiguity(),
    };

    private string Ambiguity()
    {
        StringBuilder message = new();
        message.Append($"ambiguous at {Cost} lookup(s) — bracket an argument to choose:");
        foreach (var reading in Readings) message.Append($"{Environment.NewLine}    {reading}");
        return message.ToString();
    }
}

internal enum ResolutionKind { NoParse, Resolved, Ambiguous, TooLong }
