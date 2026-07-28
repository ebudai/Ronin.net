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
    private const int MaxBindingPower = 30;

    public Resolver(SymbolTable symbols, int patternBindingPower = 7)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        if (patternBindingPower < 0 || patternBindingPower > MaxBindingPower)
            throw new ArgumentOutOfRangeException(nameof(patternBindingPower));

        this.symbols = symbols;
        PatternBindingPower = patternBindingPower;
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

        var n = lexemes.Count;
        closed = NewTable(n);
        open = NewTable(n);
        expressions = new Cell[n + 1, n + 1, MaxBindingPower + 2];
        for (var i = 0; i <= n; ++i)
            for (var j = 0; j <= n; ++j)
                for (var m = 0; m <= MaxBindingPower + 1; ++m)
                    expressions[i, j, m] = new Cell();

        for (var width = 1; width <= n; ++width)
        {
            for (var i = 0; i + width <= n; ++i)
            {
                var j = i + width;
                Atoms(lexemes, i, j);

                // descending so that a narrower minimum is filled after the
                // wider ones it may draw on at the same span
                for (var m = MaxBindingPower + 1; m >= 0; --m) Expression(lexemes, i, j, m);
            }
        }

        var top = expressions[0, n, 0];
        if (top.TryBest(out var best) is false) return Resolution.NoParse;

        return best.Count > 1 ? Resolution.Ambiguous(best.Cost, top.Readings)
                              : Resolution.Resolved(best.Cost, best.Node);
    }

    public Resolution Resolve(string source) => Resolve(Lexeme.Split(source));

    private static Cell[,] NewTable(int n)
    {
        var table = new Cell[n + 1, n + 1];
        for (var i = 0; i <= n; ++i)
            for (var j = 0; j <= n; ++j)
                table[i, j] = new Cell();
        return table;
    }

    /// <summary>
    ///     Fills the atom tables for one span. <see cref="closed"/> holds atoms
    ///     complete in themselves; <see cref="open"/> holds pattern calls ending
    ///     in an unbracketed trailing argument, which are the ones precedence has
    ///     to constrain.
    /// </summary>
    private void Atoms(IReadOnlyList<Lexeme> lexemes, int i, int j)
    {
        var cell = closed[i, j];

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

        foreach (var pattern in symbols.Patterns)
        {
            var target = pattern.IsOpenEnded ? open[i, j] : cell;
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
            if (expressions[start, end, 0].TryBest(out var part) is false) return;

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
            if (expressions[position, end, PatternBindingPower].TryBest(out var trailing))
                yield return (trailing.Cost, [trailing.Node], trailing.Count);
            yield break;
        }

        for (var split = position + 1; split <= end; ++split)
        {
            // medial args cross any operator
            if (expressions[position, split, 0].TryBest(out var argument) is false) continue;
            foreach (var (cost, arguments, count) in Match(pattern, segment + 1, lexemes, split, end))
                yield return (argument.Cost + cost, [argument.Node, .. arguments], Cell.Saturating(argument.Count * count));
        }
    }

    private void Expression(IReadOnlyList<Lexeme> lexemes, int i, int j, int minimum)
    {
        var cell = expressions[i, j, minimum];
        cell.Merge(closed[i, j]);

        // an open pattern call returns at PatternBindingPower, so it is only
        // available where no tighter minimum is demanded
        if (minimum <= PatternBindingPower) cell.Merge(open[i, j]);

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

            if (expressions[i, k, leftminimum].TryBest(out var left) is false) continue;
            if (expressions[k + 1, j, rightminimum].TryBest(out var right) is false) continue;

            cell.Offer(left.Cost + right.Cost,
                       new Node.Operation(left.Node, lexemes[k].Text, right.Node),
                       Cell.Saturating(left.Count * right.Count));
        }
    }

    private readonly SymbolTable symbols;
    private Cell[,] closed;
    private Cell[,] open;
    private Cell[,,] expressions;

    /// <summary>
    ///     Cheapest cost for a span, and how many derivations achieve it. The
    ///     count has to propagate through <see cref="Merge"/> or a tie inside a
    ///     subspan disappears the moment a larger span uses it.
    /// </summary>
    private sealed class Cell
    {
        public int Cost { get; private set; } = int.MaxValue;

        public bool IsEmpty => order.Count is 0;

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
        private readonly List<Node> order = [];
        private readonly Dictionary<string, long> derivations = new();
    }
}

/// <summary>The cheapest reading of one span, and how many derivations reach it.</summary>
internal readonly record struct Best(int Cost, Node Node, long Count);

internal enum LexemeKind { Word, Number, Symbol, Open, Close, Separator }

internal readonly record struct Lexeme(LexemeKind Kind, string Text)
{
    /// <summary>
    ///     Convenience splitter for tests and scratch work. Production input
    ///     comes from <c>Lexer</c> via <c>LexemeExtensions.ToLexemes</c>, which
    ///     already classifies words and symbols separately.
    /// </summary>
    public static List<Lexeme> Split(string source)
    {
        List<Lexeme> lexemes = [];
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            if (char.IsWhiteSpace(c)) { ++i; continue; }

            if (c is '(') { lexemes.Add(new(LexemeKind.Open, "(")); ++i; continue; }
            if (c is ')') { lexemes.Add(new(LexemeKind.Close, ")")); ++i; continue; }
            if (c is ',') { lexemes.Add(new(LexemeKind.Separator, ",")); ++i; continue; }

            var start = i;
            if (char.IsDigit(c))
            {
                while (i < source.Length && (char.IsDigit(source[i]) || source[i] is '.')) ++i;
                lexemes.Add(new(LexemeKind.Number, source[start..i]));
                continue;
            }
            if (char.IsLetter(c) || c is '_')
            {
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_')) ++i;
                lexemes.Add(new(LexemeKind.Word, source[start..i]));
                continue;
            }
            while (i < source.Length
                && char.IsWhiteSpace(source[i]) is false
                && char.IsLetterOrDigit(source[i]) is false
                && source[i] is not ('(' or ')' or ',')) ++i;
            lexemes.Add(new(LexemeKind.Symbol, source[start..i]));
        }
        return lexemes;
    }
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

        Segments = segments;
    }

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

internal sealed record Operator(int BindingPower, bool IsLeftAssociative = true);

/// <summary>Names and patterns in scope, plus the fixed operator table.</summary>
internal sealed class SymbolTable
{
    public HashSet<string> Names { get; } = [];

    public List<Pattern> Patterns { get; } = [];

    /// <summary>
    ///     Fixed at language design time. No user defined operators, which is
    ///     what keeps symbol lexing context free and disarms the maximal munch
    ///     trap that «a&lt;-b» represents in Haskell.
    /// </summary>
    ///
    /// <remarks>
    ///     Every entry must be one the lexer can actually produce. <c>Symbol.Lex</c>
    ///     advances a single character, so anything longer needs a
    ///     <c>Symbol.Special</c> of its own — otherwise the entry is dead and the
    ///     statements using it silently fail to resolve.
    /// </remarks>
    public Dictionary<string, Operator> Operators { get; } = new()
    {
        ["+"] = new(10),
        ["-"] = new(10),
        ["*"] = new(20),
        ["/"] = new(20),
    };

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

internal enum ResolutionKind { NoParse, Resolved, Ambiguous }
