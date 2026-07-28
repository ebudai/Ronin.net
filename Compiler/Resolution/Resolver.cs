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
                              : Resolution.Resolved(best.Cost, best.Reading);
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

        if (j - i is 1 && lexemes[i].Kind is LexemeKind.Number) cell.Offer(0, lexemes[i].Text);

        if (AllWords(lexemes, i, j))
        {
            var name = string.Join(' ', Enumerable.Range(i, j - i).Select(k => lexemes[k].Text));
            if (symbols.Names.Contains(name)) cell.Offer(1, $"«{name}»");
        }

        // a bracketed substatement is one lookup however large it is, and it is
        // CLOSED, which is what lets «(compute total for a) + b» resolve
        if (j - i >= 2 && lexemes[i].Kind is LexemeKind.Open && lexemes[j - 1].Kind is LexemeKind.Close)
        {
            if (expressions[i + 1, j - 1, 0].TryBest(out var inner)) cell.Offer(1 + inner.Cost, $"⟨{inner.Reading}⟩");
        }

        foreach (var pattern in symbols.Patterns)
        {
            var target = pattern.IsOpenEnded ? open[i, j] : cell;
            foreach (var (cost, reading, count) in Match(pattern, 0, lexemes, i, j))
                target.Offer(1 + cost, reading, count);
        }
    }

    private static bool AllWords(IReadOnlyList<Lexeme> lexemes, int i, int j)
    {
        for (var k = i; k < j; ++k) if (lexemes[k].Kind is not LexemeKind.Word) return false;
        return true;
    }

    private IEnumerable<(int Cost, string Reading, long Count)> Match(
        Pattern pattern, int segment, IReadOnlyList<Lexeme> lexemes, int position, int end)
    {
        if (segment == pattern.Segments.Count)
        {
            if (position == end) yield return (0, string.Empty, 1);
            yield break;
        }

        var word = pattern.Segments[segment];
        if (word is not null)
        {
            if (position < end && lexemes[position].Kind is LexemeKind.Word && lexemes[position].Text == word)
                foreach (var (cost, reading, count) in Match(pattern, segment + 1, lexemes, position + 1, end))
                    yield return (cost, $"{word} {reading}".TrimEnd(), count);
            yield break;
        }

        if (segment == pattern.Segments.Count - 1)
        {
            // trailing argument: reaches the end of the span, parsed at the
            // pattern's own binding power
            if (expressions[position, end, PatternBindingPower].TryBest(out var trailing))
                yield return (trailing.Cost, trailing.Reading, trailing.Count);
            yield break;
        }

        for (var split = position + 1; split <= end; ++split)
        {
            // medial args cross any operator
            if (expressions[position, split, 0].TryBest(out var argument) is false) continue;
            foreach (var (cost, reading, count) in Match(pattern, segment + 1, lexemes, split, end))
                yield return (argument.Cost + cost, $"{argument.Reading} {reading}".TrimEnd(), argument.Count * count);
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
                       $"({left.Reading} {lexemes[k].Text} {right.Reading})",
                       left.Count * right.Count);
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

        public long Count => derivations.Values.Sum();

        public IReadOnlyList<string> Readings => order;

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

        public void Offer(int cost, string reading, long count = 1)
        {
            if (IsEmpty || cost < Cost)
            {
                Cost = cost;
                order.Clear();
                derivations.Clear();
                order.Add(reading);
                derivations[reading] = count;
                return;
            }
            if (cost != Cost) return;
            if (derivations.ContainsKey(reading) is false) order.Add(reading);
            derivations[reading] = derivations.GetValueOrDefault(reading) + count;
        }

        public void Merge(Cell other)
        {
            if (other.IsEmpty) return;
            foreach (var reading in other.order) Offer(other.Cost, reading, other.derivations[reading]);
        }

        // Dictionary is NOT insertion ordered in .NET, and Reading must be
        // deterministic, so order is tracked explicitly alongside the counts.
        private readonly List<string> order = new();
        private readonly Dictionary<string, long> derivations = new();
    }
}

/// <summary>The cheapest reading of one span, and how many derivations reach it.</summary>
internal readonly record struct Best(int Cost, string Reading, long Count);

internal enum LexemeKind { Word, Number, Symbol, Open, Close }

internal readonly record struct Lexeme(LexemeKind Kind, string Text)
{
    /// <summary>
    ///     Convenience splitter for tests and scratch work. Production input
    ///     comes from <c>Lexer</c> via <c>LexemeExtensions.ToLexemes</c>, which
    ///     already classifies words and symbols separately.
    /// </summary>
    public static List<Lexeme> Split(string source)
    {
        List<Lexeme> lexemes = new();
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            if (char.IsWhiteSpace(c)) { ++i; continue; }

            if (c is '(') { lexemes.Add(new(LexemeKind.Open, "(")); ++i; continue; }
            if (c is ')') { lexemes.Add(new(LexemeKind.Close, ")")); ++i; continue; }

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
                && source[i] is not ('(' or ')')) ++i;
            lexemes.Add(new(LexemeKind.Symbol, source[start..i]));
        }
        return lexemes;
    }
}

/// <summary>A word pattern. A null segment is a hole.</summary>
internal sealed class Pattern
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
        => new(pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s is "_" ? null : s).ToArray());

    public IReadOnlyList<string> Segments { get; }

    /// <summary>True when the last segment is a hole, so the call has an unbracketed trailing argument.</summary>
    public bool IsOpenEnded => Segments[^1] is null;

    /// <summary>The literal words before the first hole. Anchor runs must be prefix free across a scope.</summary>
    public IReadOnlyList<string> Anchor => Segments.TakeWhile(s => s is not null).ToArray();

    /// <summary>Literal segments after the first hole. These are reserved against multi-word names.</summary>
    public IEnumerable<string> Glue => Segments.Skip(Anchor.Count).Where(s => s is not null);

    public override string ToString() => string.Join(' ', Segments.Select(s => s ?? "(_)"));
}

internal sealed record Operator(int BindingPower, bool IsLeftAssociative = true);

/// <summary>Names and patterns in scope, plus the fixed operator table.</summary>
internal sealed class SymbolTable
{
    public HashSet<string> Names { get; } = new();

    public List<Pattern> Patterns { get; } = new();

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

    public SymbolTable WithNames(params string[] names)
    {
        foreach (var name in names) Names.Add(name);
        return this;
    }

    public SymbolTable WithPatterns(params string[] patterns)
    {
        foreach (var pattern in patterns) Patterns.Add(Pattern.Parse(pattern));
        return this;
    }

    /// <summary>
    ///     The two scope-wide well-formedness rules. Both were found by exhaustive
    ///     search, not inspection, and without them the resolver reports ties that
    ///     no bracketing can repair.
    /// </summary>
    public IEnumerable<string> Validate()
    {
        // R6: anchor runs must be prefix free, or «b (_)» and «b b (_)» tie on
        // «b b b a» with no name involved at all
        foreach (var a in Patterns)
        {
            foreach (var b in Patterns)
            {
                if (ReferenceEquals(a, b)) continue;
                if (a.Anchor.Count >= b.Anchor.Count) continue;
                if (a.Anchor.SequenceEqual(b.Anchor.Take(a.Anchor.Count)))
                    yield return $"anchor of «{a}» is a prefix of «{b}»; one must be respelled";
            }
        }

        // R5: a multi-word name may not contain pattern glue, or introducing a
        // name silently re-resolves statements that already worked
        var glue = Patterns.SelectMany(p => p.Glue).ToHashSet();
        foreach (var name in Names)
        {
            var words = name.Split(' ');
            if (words.Length < 2) continue;
            foreach (var word in words.Where(glue.Contains))
                yield return $"name «{name}» contains pattern glue «{word}»";
        }
    }
}

internal readonly record struct Resolution(ResolutionKind Kind, int Cost, IReadOnlyCollection<string> Readings)
{
    public static readonly Resolution NoParse = new(ResolutionKind.NoParse, 0, Array.Empty<string>());

    public static Resolution Resolved(int cost, string reading) => new(ResolutionKind.Resolved, cost, new[] { reading });

    public static Resolution Ambiguous(int cost, IReadOnlyCollection<string> readings)
        => new(ResolutionKind.Ambiguous, cost, readings.ToArray());

    public string Reading => Readings.FirstOrDefault() ?? string.Empty;

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
