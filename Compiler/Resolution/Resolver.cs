// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            // The same shape may be present when a caller folds every built-in
            // into a table. Its hole is not an ordinary expression hole and is
            // handled by Previous below, so admitting it here as a normal call
            // would reopen «old (x + 1)» through a second path.
            if (pattern.Equals(SymbolTable.Previous)) continue;

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

        return best.Count > 1 ? Resolution.Ambiguous(best.Cost, best.Witness)
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

        if (CanName(lexemes, i, j))
        {
            var name = string.Join(' ', Enumerable.Range(i, j - i).Select(k => lexemes[k].Text));
            if (symbols.Names.Contains(name)) cell.Offer(1, new Node.Name(name));
        }

        // a bracketed substatement is one lookup however large it is, and it is
        // CLOSED, which is what lets «(compute total for a) + b» resolve
        if (j - i >= 2 && lexemes[i].Kind is LexemeKind.Open && lexemes[j - 1].Kind is LexemeKind.Close)
        {
            Group(lexemes, cell, i + 1, j - 1, collection: lexemes[i].Text is "[");
        }

        // A language pattern with a stricter hole than an ordinary expression.
        // Offered as CLOSED: once the hole is known to be one bare reference,
        // its extent is fixed, so «old x + 1» can only be «(old x) + 1».
        // Treating it as an ordinary open-ended pattern would either swallow
        // «x + 1» (which is not a reference) or make «old x» unavailable as
        // the left operand at arithmetic binding power.
        Previous(lexemes, cell, i, j);

        // Only the patterns whose first word is the one actually sitting at «i».
        // Every pattern begins with a word — a leading hole is refused at
        // construction, and THIS is one of the reasons why: a pattern with no
        // first word has no key here, so admitting one means either indexing it
        // by something else or going back to the table walk per pattern per span
        // that this replaced.
        if (lexemes[i].Kind is not LexemeKind.Word) return;
        if (anchored.TryGetValue(lexemes[i].Text, out var candidates) is false) return;

        foreach (var pattern in candidates)
        {
            var target = pattern.IsOpenEnded ? open[Span(i, j)] : cell;
            foreach (var (cost, arguments, count, witness) in Match(pattern, 0, lexemes, i, j))
                target.Offer(1 + cost, new Node.Call(pattern, arguments), count, witness);
        }
    }

    /// <summary>
    ///     Offers «old (_)» only when its argument is a bare reactive name,
    ///     optionally bracketed. The resulting node retains the name rather than
    ///     evaluating it, so evaluation can read the graph's shadow without
    ///     first recording an edge on the current value.
    /// </summary>
    private void Previous(IReadOnlyList<Lexeme> lexemes, Cell cell, int i, int j)
    {
        if (j - i < 2) return;
        if (lexemes[i] is not { Kind: LexemeKind.Word } || lexemes[i].Text != SymbolTable.Old) return;

        var from = i + 1;
        var to = j;
        var bracketed = false;

        if (lexemes[from] is { Kind: LexemeKind.Open, Text: "(" }
            && lexemes[j - 1] is { Kind: LexemeKind.Close, Text: ")" })
        {
            ++from;
            --to;
            bracketed = true;
        }

        if (from == to || CanName(lexemes, from, to) is false) return;

        var name = string.Join(' ', lexemes.Skip(from).Take(to - from).Select(lexeme => lexeme.Text));
        if (symbols.Names.Contains(name) is false || symbols.IsReactive(name) is false) return;

        Node argument = new Node.Name(name);
        if (bracketed) argument = new Node.Group([argument]);

        cell.Offer(bracketed ? 3 : 2, new Node.Previous(name, argument));
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
    private void Group(IReadOnlyList<Lexeme> lexemes, Cell cell, int from, int to, bool collection)
    {
        // An empty COLLECTION is a value — the list with nothing in it — where an
        // empty grouping is not: «()» is brackets round no expression and there
        // is nothing for it to mean. The loop below cannot say that, because it
        // asks for an expression between each pair of separators and an empty
        // span has none.
        if (collection && from == to)
        {
            cell.Offer(1, new Node.Group([], collection: true));
            return;
        }

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
        IReadOnlyList<string> witness = [];
        var count = 1L;
        List<Node> parts = [];
        var start = from;

        // A TRAILING separator ends the last part rather than starting an empty
        // one. The aggregate permits it and the guide's examples use it, so
        // «[10, ]» compiles with no finding and then reached here, where the
        // span after the last comma is empty and the whole group was refused —
        // a language-valid list that would not resolve.
        //
        // Only the last, and only at top level: a leading or doubled separator
        // still asks for an expression that is not there and is still refused.
        if (separators.Count is not 0 && separators[^1] == to - 1)
        {
            to = separators[^1];
            separators.RemoveAt(separators.Count - 1);
        }

        foreach (var end in separators.Append(to))
        {
            // an empty part — a leading or doubled separator — is not a
            // substatement at all
            if (Expressions(start, end, 0).TryBest(out var part) is false) return;

            cost += part.Cost;

            // Saturate every step, as Match and Expression already do. Saturating
            // only at the end left a raw product across the parts, and a group of
            // 63 independently ambiguous parts reached 2^63 — which wraps to
            // negative, is duly reported as fewer than two derivations, and
            // returns a genuine tie as Resolved.
            count = Cell.Saturating(count * part.Count);
            witness = Best.Either(witness, part.Witness);

            parts.Add(part.Node);
            start = end + 1;
        }

        cell.Offer(1 + cost, new Node.Group(parts, collection), count, witness);
    }

    /// <summary>
    ///     The name a binding hole declares and how far it reaches, or null where
    ///     the span does not start with one.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     One word, or several inside ROUND brackets — the rule already settled
    ///     for new names, and the same one the grammar's own loop parser applies.
    ///     Nothing is looked up, because a declaration is not a reference.
    ///     </para>
    ///     <para>
    ///     Extent and validity together, in one walk, because they were two: the
    ///     extent was measured by matching brackets and the span was then scored
    ///     as an ordinary expression. An expression is happy to be a literal, an
    ///     operation, several values or any bracket at all, so «for each (3) in
    ///     banks», «for each (a + b) in banks» and «for each [x] in banks» all
    ///     resolved to something no loop could bind.
    ///     </para>
    ///     <para>
    ///     The bracket kind is checked by its text and not by
    ///     <see cref="LexemeKind"/>, which erases it: «[», «{» and «(» are all
    ///     Open, so nothing was checking that the brackets even matched.
    ///     </para>
    /// </remarks>
    private static Node.Binding Binding(IReadOnlyList<Lexeme> lexemes, int i, int j, out int only)
    {
        only = i;

        if (i >= j) return null;

        // A keyword that announces a production may not begin a name, which is
        // the grammar's rule and now this one's — asked of the same lexemes
        // rather than restated. Every resolver word used to be bindable, so
        // «for each if in banks» and «for each part of in banks» resolved
        // cleanly while the parser called them Malformed.
        if (lexemes[i].Announces) return null;

        if (lexemes[i].Kind is LexemeKind.Word)
        {
            only = i + 1;
            return new Node.Binding(lexemes[i].Text);
        }

        if (lexemes[i] is not { Kind: LexemeKind.Open, Text: "(" }) return null;

        var depth = 0;

        for (var at = i; at < j; ++at)
        {
            if (lexemes[at].Kind is LexemeKind.Open) ++depth;
            else if (lexemes[at].Kind is LexemeKind.Close && --depth is 0)
            {
                if (lexemes[at].Text is not ")") return null;
                if (at - i is 1) return null;
                if (CanName(lexemes, i + 1, at) is false) return null;

                // Inside the brackets it is still a name, so the same rule holds
                // about the word it begins with — «for each (if ready) in banks»
                // resolved while the parser called it Malformed.
                if (lexemes[i + 1].Announces) return null;

                only = at + 1;
                return new Node.Binding(string.Join(' ', lexemes.Skip(i + 1)
                                                                .Take(at - i - 1)
                                                                .Select(lexeme => lexeme.Text)));
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether a span could be a name, which is to say whether it is words
    ///     and nothing else.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     LOAD-BEARING, far beyond this method. Every argument that a glue word
    ///     is safe to leave unreserved rests on this one line: a name cannot
    ///     contain a bracket or a symbol, so it cannot STRADDLE one, so a word
    ///     sitting beside a bracket cannot be swallowed by a longer name. That is
    ///     why «send (hello) to alice» is safe where «send hello to alice» is
    ///     not, and it is the whole of why bracket-delimited and symbol-separated
    ///     patterns cost nothing.
    ///     </para>
    ///     <para>
    ///     The exhaustive searches did not verify that. They counted TIES, and
    ///     ties are all any of them measured — 2,382,240 resolutions over
    ///     anchor-first word patterns with no brackets, 45,131,520 over the
    ///     narrower bracket runs, and 294,333,696 over pattern pairs at three
    ///     units. The no-capture property is structural and comes from here, so
    ///     widening what may be part of a name would invalidate it silently and
    ///     no fuzzer would notice: the resolutions would stay unique, they would
    ///     simply be unique and wrong.
    ///     <c>ANameIsWordsAndNothingElse</c> is the test that would.
    ///     </para>
    /// </remarks>
    internal static bool CanName(IReadOnlyList<Lexeme> lexemes, int i, int j)
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
    private IEnumerable<(int Cost, IReadOnlyList<Node> Arguments, long Count, IReadOnlyList<string> Witness)> Match(
        Pattern pattern, int segment, IReadOnlyList<Lexeme> lexemes, int position, int end)
    {
        if (segment == pattern.Segments.Count)
        {
            if (position == end) yield return (0, [], 1, []);
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

        // A pinned hole is exactly one word, which is what makes the split
        // around it structural rather than scored: nothing can grow across it,
        // so the word after it needs no reserving. Enforced HERE and not only in
        // the glue calculation, or the pattern would claim a guarantee the
        // resolver does not give.
        var pinned = pattern.Pinned.Contains(segment);

        // One word, or one bracketed group where a word will not do. Both are
        // determinate in EXTENT — the word by being one token, the group by
        // being matched — which is the property that fixes the split and makes
        // the word after it safe to leave unreserved.
        if (pinned)
        {
            // A BINDING occurrence, not a value. The hole declares the name, so
            // there is nothing to look up and nothing to score — and resolving
            // it as an expression meant «for each bank in banks» only resolved
            // when «bank» was ALREADY declared, which is the one table the real
            // path can never present: the loop is what declares it.
            //
            // It also over-accepted in the other direction, because an
            // expression is happy to be a literal, an operator, several values,
            // or any bracket at all: «for each (3) in banks», «for each (a + b)
            // in banks» and «for each [x] in banks» all resolved. None is a name
            // anything could bind.
            if (Binding(lexemes, position, end, out var only) is not Node.Binding name) yield break;

            if (segment == pattern.Segments.Count - 1)
            {
                if (only == end) yield return (0, [name], 1, []);
                yield break;
            }

            foreach (var (bound, arguments, count, witness) in Match(pattern, segment + 1, lexemes, only, end))
                yield return (bound, [name, .. arguments], count, witness);

            yield break;
        }

        if (segment == pattern.Segments.Count - 1)
        {
            // trailing argument: reaches the end of the span, parsed at the
            // pattern's own binding power
            if (Expressions(position, end, PatternBindingPower).TryBest(out var trailing))
                yield return (trailing.Cost, [trailing.Node], trailing.Count, Best.Pair(trailing.Witness));
            yield break;
        }

        for (var split = position + 1; split <= end; ++split)
        {
            // medial args cross any operator
            if (Expressions(position, split, 0).TryBest(out var argument) is false) continue;
            foreach (var (cost, arguments, count, witness) in Match(pattern, segment + 1, lexemes, split, end))
                yield return (argument.Cost + cost,
                              [argument.Node, .. arguments],
                              Cell.Saturating(argument.Count * count),
                              Best.Either(argument.Witness, witness));
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
                // A WORD may be one too. «otherwise» is an infix form and not
                // a pattern — a pattern with a leading hole has an empty anchor
                // run, which R6 refuses against every anchored pattern there is
                // — so it lives here, beside «+», where an operator is
                // recognised without asking the symbol table what is declared.
                case LexemeKind.Symbol or LexemeKind.Word when depth is 0 && k > i && k < j - 1: break;
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
                       Cell.Saturating(left.Count * right.Count),
                       Best.Either(left.Witness, right.Witness));
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

            best = new Best(Cost, order[0], Count, Witness);
            return true;
        }

        /// <summary>
        ///     Two readings and no more, because two prove a tie and explain it.
        /// </summary>
        ///
        /// <remarks>
        ///     Its own pair when it has one — this cell IS where the tie is. Only
        ///     when it does not does the tie come from further in, and then the
        ///     witness travelled up with the derivation that carried the count.
        /// </remarks>
        /// <remarks>
        ///     Typed «Owned.Kept», which is what stops this branch quietly
        ///     becoming an ordinary collection again. It was one expression
        ///     inside a private nested type, so reverting it left every test
        ///     green — «Best» owns whatever it is handed and repairs both cases
        ///     before anything downstream can tell them apart. A collection
        ///     expression cannot satisfy this type, so the compiler refuses what
        ///     no test could see.
        /// </remarks>
        /// <remarks>
        ///     CHEAPEST FIRST, because the diagnostic offers these in order and
        ///     the likeliest reading should be the first one a person sees. That
        ///     is the whole of what cost does now: it may order the suggestions
        ///     and it may never choose among them. The moment it chooses, every
        ///     silent capture this replaced comes back looking like a feature.
        /// </remarks>
        private Owned.Kept<string> Witness
            => order.Count > 1
             ? Best.Readings([.. order.OrderBy(node => costs[node])])
             : witnesses[order[0]];

        /// <summary>Two is as many as anything needs to be counted.</summary>
        public static long Saturating(long count) => count < 2 ? count : 2;

        public void Offer(int cost, Node node, long count = 1, IReadOnlyList<string> witness = null)
        {
            // Keyed by SHAPE and not by rendering. It was keyed by the
            // rendering, under a comment that made it a claim — two derivations
            // that read the same way ARE the same reading — and a nested call
            // renders without delimiting itself, so «print (send a to b)» and
            // «print (send a) to b» arrived here as the same string. The second
            // was dropped as a duplicate and the statement came back Resolved
            // with one meaning out of two.
            // Admitted HERE, so what a cell stores is owned however it arrived.
            // The storage carries the guarantee rather than each writer
            // remembering to.
            var offered = witness is null ? Owned.None<string>() : Owned.Copy(witness);

            // EVERY derivation is kept, not only the cheapest. Minimum lookup
            // used to discard the dearer ones here, which is what made a
            // strictly cheaper reading win in silence — «send time to live»
            // simply meant the name, and nothing said so.
            //
            // Ambiguity is the error now, so what this holds becomes "how many
            // derivations, and what they are" rather than "the cheapest, and how
            // many achieve it". Same table and same asymptotics: measured
            // identical at 149 lexemes, 26.2 MB either way, because the
            // derivations it used to drop were few — adjacency does not compose
            // in this grammar, so there are no cut points to multiply.
            if (IsEmpty)
            {
                Cost = cost;

                // On first offer, not at construction. Most cells in the table
                // are never offered anything — a span that is not an expression
                // still gets one per binding power — so eagerly allocating both
                // collections was two objects per cell for nothing.
                order ??= [];
                costs ??= new(Node.Same);
                derivations ??= new(Node.Same);
                witnesses ??= new(Node.Same);

                order.Clear();
                costs.Clear();
                derivations.Clear();
                witnesses.Clear();
            }

            // Kept for RANKING and never for choosing. The cheapest reading is
            // the one to offer first in the diagnostic, and the moment it is
            // allowed to decide instead, every silent capture this removes comes
            // back looking like a feature.
            if (cost < Cost) Cost = cost;

            if (derivations.ContainsKey(node) is false) order.Add(node);

            // The CHEAPEST way to reach a reading, where two derivations render
            // alike: they are one reading, so they rank once and rank at their
            // best.
            costs[node] = costs.TryGetValue(node, out var already) ? System.Math.Min(already, cost) : cost;

            // The LARGER and not the sum, because two derivations that read the
            // same way are the same reading — which this said in a comment and
            // then did not do. Two identical patterns in a table made «take x»
            // count two while leaving one rendering in order, so it came back
            // Ambiguous with no readings at all: a tie reported between a
            // statement and itself, with nothing to show for it.
            derivations[node] = System.Math.Max(derivations.GetValueOrDefault(node), count);
            witnesses[node] = Best.Either(witnesses.GetValueOrDefault(node) ?? Owned.None<string>(), offered);
        }

        public void Merge(Cell other)
        {
            if (other.IsEmpty) return;

            // ITS OWN cost, not the cell's. «other.Cost» is the cheapest
            // anything in that cell reaches, so merging flattened every reading
            // to the minimum and the ranking became insertion order — with the
            // dearer pattern declared first, the dearer reading was offered
            // first. Cost may no longer choose, and it had quietly stopped
            // ordering either.
            foreach (var node in other.order)
            {
                Offer(other.costs[node], node, other.derivations[node], other.witnesses[node]);
            }
        }

        // Dictionary is NOT insertion ordered in .NET, and the chosen reading must
        // be deterministic, so order is tracked explicitly alongside the counts.
        // Both are null until something is offered, which is what «IsEmpty» reads.
        private List<Node> order;

        /// <summary>What each reading costs, for ordering them.</summary>
        ///
        /// <remarks>
        ///     Kept per READING now that the cell holds every derivation rather
        ///     than only the cheapest. Cost used to decide which survived, so one
        ///     number was enough; it ranks them instead, and a rank needs one
        ///     number each.
        /// </remarks>
        private Dictionary<Node, int> costs;

        private Dictionary<Node, long> derivations;
        private Dictionary<Node, Owned.Kept<string>> witnesses;
    }
}

/// <summary>The cheapest reading of one span, and how many derivations reach it.</summary>
///
/// <param name="Witness">
///     The two readings that make <paramref name="Count"/> two, which is not
///     always this span's own pair. A parent combining children keeps one node
///     and a count, so an ambiguity inside a bracket, an operator or an outer
///     call arrives here as "two derivations" with nothing to show — and the
///     message then invited the reader to choose between one reading and
///     nothing. Empty when the span is unambiguous.
/// </param>
internal readonly record struct Best(int Cost, Node Node, long Count, IReadOnlyList<string> Witness)
{
    /// <remarks>
    ///     Wrapped, though the resolver never hands over a writable one: the
    ///     cell's witness is either its own renderings or a bounded pair, and
    ///     both are already read-only. So this is the invariant made
    ///     unconditional rather than left true by every caller happening to keep
    ///     it.
    ///
    ///     Kept and not copied, because the resolver's producers build owned
    ///     witnesses through «Owned.Of» — so this is the type test and nothing
    ///     more. It said it copied only writable values and never in the
    ///     resolver, which stopped being true when the rule stopped asking about
    ///     writability: every unmarked non-empty witness was copied here, and a
    ///     propagated one was copied again at each level it rose through.
    /// </remarks>
    public IReadOnlyList<string> Witness { get; } = Owned.Copy(Witness);

    /// <summary>
    ///     The first witness there is, bounded, since one pair explains a tie.
    /// </summary>
    ///
    /// <remarks>
    ///     Bounded HERE and not at the tie itself, which is the whole
    ///     distinction. A cell that is ambiguous hands out every reading it has,
    ///     because each one is a bracketing the reader could choose and listing
    ///     two of three would hide a repair. A witness travelling UP through a
    ///     bracket, an operator or an outer call is doing a different job — it
    ///     proves and explains a tie the parent cannot see — and two readings
    ///     prove it as well as ten.
    /// </remarks>
    public static Owned.Kept<string> Either(IReadOnlyList<string> witness, IReadOnlyList<string> otherwise)
        => Pair(witness.Count is 0 ? otherwise : witness);

    /// <summary>
    ///     A cell's own tied readings, owned where they are made.
    /// </summary>
    ///
    /// <remarks>
    ///     NAMED, and not left inline in the cell that calls it. It was one
    ///     expression inside a private nested type, so nothing could reach it —
    ///     reverting it to an ordinary collection left every test green,
    ///     including the one written to protect exactly this, because «Best»
    ///     owns whatever it is handed and repairs both cases before anything
    ///     downstream can tell them apart. A producer whose only witness is a
    ///     112-byte difference needs somewhere a test can stand.
    /// </remarks>
    public static Owned.Kept<string> Readings(IReadOnlyList<Node> order)
        => Owned.Of(order, static node => node.ToString());

    /// <summary>At most two, which is what a parent carries.</summary>
    ///
    /// <remarks>
    ///     The short case returned the CALLER'S list, so what a parent carried
    ///     was whatever object it was handed — writable when the caller built a
    ///     «List», read-only when it wrote a collection expression. Same
    ///     declared type either way, which is how it went unnoticed.
    ///
    ///     OWNED on the way out, both cases, so the cell that stores it and the
    ///     «Best» that carries it keep the one value rather than copying it
    ///     again. A witness travelling up through brackets was copied once per
    ///     level before this.
    ///
    ///     The two VALUES, and not a collection holding them. Three shapes
    ///     were measured before this one: «Take(2)» at 152 bytes a call, a
    ///     collection expression at 128, and handing over the elements
    ///     themselves at 64. The first comparison stopped at the second, which
    ///     is how a choice between two can look settled while the answer is
    ///     neither.
    /// </remarks>
    public static Owned.Kept<string> Pair(IReadOnlyList<string> witness)
        => witness.Count > 2 ? Owned.Of(witness[0], witness[1]) : Owned.Copy(witness);
}

internal enum LexemeKind { Word, Number, Symbol, Open, Close, Separator }

/// <param name="Announces">
///     Whether this word is a keyword that introduces a production, which is the
///     one thing a name may not BEGIN with. Carried rather than re-derived,
///     because the resolver has no tokens left to ask — and a keyword is an
///     ordinary word everywhere else, so it cannot be a <see cref="LexemeKind"/>
///     of its own without taking «var ready if needed» out of the language.
/// </param>
internal readonly record struct Lexeme(LexemeKind Kind, string Text, bool Announces = false)
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
    public Pattern(IReadOnlyList<string> segments) : this(segments, []) { }

    /// <param name="pinned">
    ///     The holes fixed to exactly one token. A pinned hole is determinate in
    ///     EXTENT — nothing can grow leftward or rightward across it — so a word
    ///     beside one cannot be swallowed and needs no reserving. That is what
    ///     makes «for each «one word» in (_)» cost nothing where «for each (_) in
    ///     (_)» costs «in».
    ///
    ///     Determinate in extent is not determinate in IDENTITY: a pinned hole
    ///     matches any word, so in LEADING position it would collide with every
    ///     word-anchored pattern. «&lt;_&gt; b» beside «a (_)» reads «a b» two
    ///     ways. Interior only, which is where the loop wants it.
    /// </param>
    public Pattern(IReadOnlyList<string> segments, IReadOnlyCollection<int> pinned)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(pinned);
        if (segments.Count is 0) throw new ArgumentException("pattern is empty", nameof(segments));

        // NOT left recursion, which is what this said for as long as it has
        // existed. It was copied from a backtracking enumerator, where a leading
        // hole does recurse at the same position forever — but this table fills
        // by increasing width, so a leading hole reads only strictly smaller
        // spans and terminates. A property of one instrument, written up as a
        // property of the language, and inherited by everything downstream
        // including this comment.
        //
        // What stands in the way is here rather than in the grammar, and it is
        // three things: the index below keys a pattern by its first word and a
        // leading hole has none; R6 is stated over anchor runs and a leading
        // hole has an empty one, which is a prefix of every other; and the
        // reference resolver and this one disagree about whether the resulting
        // prefix-postfix composition ties or picks. Refused until those are
        // settled, and refused for those reasons.
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

        // A pin names a HOLE, and only a hole. Neither of these was checked, so
        // «take (_)» pinned at 0 described the literal «take», and pinned at 2
        // described nothing at all — and both rendered as an ordinary «take
        // (_)», which parses back to the unpinned pattern and compares unequal
        // to what it came from. That is the round trip failing on a pattern the
        // registry can emit, from metadata that says nothing.
        foreach (var hole in pinned)
        {
            if (hole < 0 || hole >= segments.Count)
                throw new ArgumentOutOfRangeException(nameof(pinned), hole,
                                                      $"there is no segment {hole} to pin");

            if (segments[hole] is not null)
                throw new ArgumentException($"segment {hole} is the word «{segments[hole]}», and a pin fixes a hole",
                                            nameof(pinned));
        }

        // Frozen, not merely typed as read-only. «IReadOnlySet» over a HashSet
        // hands out the mutable object to anything willing to cast, and this one
        // is in the hash of a dictionary key — so mutating it after insertion
        // makes the declaration unreachable. SymbolTable.Builtins is a single
        // process-wide instance, which made that a global effect.
        Pinned = System.Collections.Frozen.FrozenSet.ToFrozenSet(pinned);

        // WRITABLE, which is the invariant that makes the round trip a property
        // rather than a hope. Every non-null segment has to be one word the
        // lexer produces, and the whole sequence has to survive being written
        // down and read back — «take 1», «take <_>» and «take +» stored things
        // no source can match, and «compute» «part» «of» stored two words that
        // re-read as the one token «part of», so the renderer reconstructed a
        // pattern the compiler had not built.
        if (Writable(Segments) is false)
        {
            throw new ArgumentException("a pattern's segments must be words the lexer produces, and must read back "
                                      + $"as themselves: «{string.Join("» «", Segments.Select(segment => segment ?? Bracketed))}» "
                                      + "does not.", nameof(segments));
        }

        // Decomposed once. A pattern is immutable, and Anchor was rebuilding its
        // array on every read — inside R6's ordered pattern-by-pattern loop,
        // where even a pair rejected on length allocated two.
        Anchor = [.. Segments.TakeWhile(segment => segment is not null)];
    }

    /// <summary>
    ///     Whether every word comes before the first hole.
    /// </summary>
    ///
    /// <remarks>
    ///     The shape a NAME can swallow whole: its words are one contiguous run
    ///     at the front, so a name may begin with exactly them and cover the
    ///     whole call. Not the same as having no glue — a pinned hole makes the
    ///     word after it free of glue while leaving the words apart, and «for
    ///     each «x» in (_)» is that: no glue, and no name is «for each in …».
    /// </remarks>
    public bool IsAnchorOnly => Anchor.Count == Segments.Count(segment => segment is not null);

    /// <summary>Which holes are fixed to one token.</summary>
    public IReadOnlySet<int> Pinned { get; }

    /// <summary>The most words and holes one pattern may have.</summary>
    public const int MaxSegments = 128;

    /// <summary>
    ///     Reads "compute total for (_)" into segments, a hole being "(_)" or a
    ///     bare "_".
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     THROUGH THE LEXER, so segmentation agrees with the compiler by
    ///     construction rather than by convention. Splitting on spaces agreed
    ///     with it only for names of single-word tokens: «part of» and «for
    ///     each» are one token each, so "take part of _" built four segments
    ///     where a call lexes to three, and the pattern was declared, printed
    ///     correctly, and could never match anything.
    ///     </para>
    ///     <para>
    ///     ROUND-TRIPS OR REFUSES, and this is what makes the first half true:
    ///     <see cref="ToString"/> writes segments separated by spaces, and
    ///     re-lexing recovers exactly the tokens they came from. It refuses
    ///     everything else. Splitting on spaces accepted «take &lt;_&gt;»,
    ///     «take 1», «take a-b» and «take +» as literal WORDS — none of which
    ///     the lexer can produce as one word, so each was a pattern nothing
    ///     could ever match, constructed in silence. A pattern that cannot match
    ///     is the forbidden third outcome as much as a wrong one is.
    ///     </para>
    /// </remarks>
    public static Pattern Parse(string pattern)
        => Read(pattern) is List<string> segments
         ? new Pattern(segments)
         : throw new ArgumentException($"«{pattern}» is not words and «{Bracketed}» holes, as the lexer reads them. "
                                     + "That is a rendering rather than a declaration.", nameof(pattern));

    /// <summary>
    ///     The segments a written pattern denotes, or null where it denotes none.
    /// </summary>
    ///
    /// <remarks>
    ///     The one place a pattern is read, so <see cref="Parse"/> and the
    ///     constructor's own round-trip invariant cannot disagree about what a
    ///     written pattern means.
    /// </remarks>
    private static List<string> Read(string pattern)
    {
        var lexemes = Lexemes.Lex(pattern);

        List<string> segments = [];

        for (var at = 0; at < lexemes.Count; ++at)
        {
            if (lexemes[at].Kind is LexemeKind.Word) { segments.Add(lexemes[at].Text); continue; }

            // «(_)» and a bare «_» are the same hole. Both are NOTATION and
            // neither is source: a declaration always names its holes, and the
            // renderer drops those names because the registry is about shape.
            // This is a parser for that notation and not for Ronin.
            if (Hole(lexemes, ref at) || lexemes[at] is { Kind: LexemeKind.Symbol, Text: Blank })
            {
                segments.Add(null);
                continue;
            }

            return null;
        }

        return segments;
    }

    /// <summary>
    ///     Whether these segments read back as themselves, which is what the
    ///     constructor requires and what a declaration is checked for first.
    /// </summary>
    public static bool Writable(IReadOnlyList<string> segments) => Reads(segments).SequenceEqual(segments);

    /// <summary>
    ///     What these segments denote once written down and read back, which is
    ///     the same sequence exactly when they are <see cref="Writable"/>.
    /// </summary>
    ///
    /// <remarks>
    ///     Never throws, because a finding that says a declaration cannot be
    ///     written has to be able to say what it becomes instead — and going
    ///     through <see cref="Parse"/> to find out meant crossing the very
    ///     constructor whose invariant was being reported.
    /// </remarks>
    public static IReadOnlyList<string> Reads(IReadOnlyList<string> segments)
        => (IReadOnlyList<string>)Read(string.Join(' ', segments.Select(segment => segment ?? Bracketed)))
                                ?.AsReadOnly() ?? [];

    /// <summary>Whether a bracketed hole starts here, consuming it if it does.</summary>
    private static bool Hole(List<Lexeme> lexemes, ref int at)
    {
        if (at + 2 >= lexemes.Count) return false;

        // By TEXT, and a matching pair. «(», «[» and «{» are all Open to the
        // resolver, so checking the kind alone quietly read «take [_]», «take
        // {_}» and even «take (_]» as the ordinary free hole — and «{_}» is
        // spoken for: the design reserves braced units for a hole kind that does
        // not exist yet, which this would have consumed in advance.
        if (lexemes[at] is not { Kind: LexemeKind.Open, Text: "(" }) return false;
        if (lexemes[at + 1] is not { Kind: LexemeKind.Symbol, Text: Blank }) return false;
        if (lexemes[at + 2] is not { Kind: LexemeKind.Close, Text: ")" }) return false;

        at += 2;
        return true;
    }

    /// <summary>A hole, written the way it is called: bracketed.</summary>
    private const string Bracketed = "(_)";

    /// <summary>A hole, written bare.</summary>
    private const string Blank = "_";

    public IReadOnlyList<string> Segments { get; }

    /// <summary>True when the last segment is a hole, so the call has an unbracketed trailing argument.</summary>
    public bool IsOpenEnded => Segments[^1] is null;

    /// <summary>The literal words before the first hole. Anchor runs must be prefix free across a scope.</summary>
    public IReadOnlyList<string> Anchor { get; }

    /// <summary>
    ///     Literal segments after the first hole, minus the ones a pinned hole
    ///     already protects. These are what a name may not contain.
    /// </summary>
    ///
    /// <remarks>
    ///     A word immediately after a pinned hole is safe: the hole is exactly
    ///     one token, so the split before that word is fixed and no name can grow
    ///     across it. Reserving it anyway is what «for each (_) in (_)» was
    ///     charging «in» for, and the charge was unnecessary.
    /// </remarks>
    public IEnumerable<string> Glue
    {
        get
        {
            for (var segment = Anchor.Count; segment < Segments.Count; ++segment)
            {
                if (Segments[segment] is null) continue;

                // «segment» starts at the anchor length, which is at least one
                // because a pattern must begin with a word — so there is always
                // a previous segment to look at.
                if (Segments[segment - 1] is null && Pinned.Contains(segment - 1)) continue;

                yield return Segments[segment];
            }
        }
    }

    /// <remarks>
    ///     A free hole renders as source — «(_)» is the bracket the call site
    ///     shows and <see cref="Parse"/> reads it back. A PINNED one renders as
    ///     PROSE, because there is no declaration syntax for it yet and any
    ///     punctuation here would be an invention: «&lt;_&gt;» was the reference
    ///     probe's display notation, and it escaped into the generated registry
    ///     and from there into a question about whether Ronin had angle
    ///     brackets. Guillemets are the compiler's own quoting for text that is
    ///     about the language rather than in it, so nothing here can be copied
    ///     into a program by mistake.
    /// </remarks>
    public override string ToString()
        => string.Join(' ', Segments.Select((segment, at) => segment ?? (Pinned.Contains(at) ? Pin : Bracketed)));

    /// <summary>What a pinned hole takes, in words, until it can be declared.</summary>
    private const string Pin = "«one word, or a bracketed name»";

    public bool Equals(Pattern other)
        => other is not null && Segments.SequenceEqual(other.Segments) && Pinned.SetEquals(other.Pinned);

    public override bool Equals(object obj) => Equals(obj as Pattern);

    /// <remarks>
    ///     <see cref="Pinned"/> is part of equality, so it has to be part of the
    ///     hash: omitting it put the pinned and unpinned spellings of the same
    ///     segments in one bucket, where they compare unequal and collide for as
    ///     long as both exist.
    /// </remarks>
    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (var segment in Segments) hash.Add(segment);
        foreach (var pinned in Pinned.Order()) hash.Add(pinned);

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
    /// <summary>
    ///     Which left values this operator replaces with its right operand. Null
    ///     where it replaces none, which is every arithmetic operator.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Not an optimisation. A body's dependencies are collected BY
    ///     evaluating it, so an operand that is evaluated is a cell that is read
    ///     and an edge that exists — «a otherwise b» would wake on «b» moving
    ///     while «a» was perfectly good, and the fallback of a fallback would be
    ///     computed for every value that never needed one.
    ///     </para>
    ///     <para>
    ///     Asked of the LEFT value, because that is the whole of what decides it
    ///     here: «otherwise» wants its right operand when the left is an error or
    ///     nothing, and in every other case the left value IS the answer. That is
    ///     also why the caller may return the left when this says no — an
    ///     operator that short-circuits to something else would need a different
    ///     question.
    ///     </para>
    ///     <para>
    ///     Being set at all says the operator INSPECTS a failure, which is a
    ///     second thing and not a coincidence: the graph adopts the first error a
    ///     body reads and applies it to whatever the body returns, so an operator
    ///     that examines one has to be handed it without the graph inheriting it
    ///     on its behalf. <c>Graph.Handling</c> is that boundary and exists for
    ///     this operator by name.
    ///     </para>
    /// </remarks>
    public Func<object, bool> Catches { get; init; }

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
    ///     Here for their SHAPES. The loop pattern describes a grammar production
    ///     whose declaring hole is pinned; «old (_)» describes a resolver atom
    ///     whose hole is constrained to a reactive name. Neither is admitted as
    ///     an ordinary expression-hole pattern through <see cref="Patterns"/>.
    ///     </para>
    ///     <para>
    ///     A loop header needs exactly one reading, and the first way to get one
    ///     was to reserve «in» against names; see LOOPSYNTAX.md, where the
    ///     alternative is not a tie but a strictly-cheaper wrong reading that
    ///     nothing flags. Pinning the declaring hole buys the same guarantee for
    ///     free: a hole fixed at one token cannot grow across the «in» that
    ///     follows it, so the split point is determined without taking a word
    ///     away from anyone, and «in» is an ordinary name again.
    ///     </para>
    ///     <para>
    ///     The loop is not in <see cref="Patterns"/>, because today it is a
    ///     grammar production and the resolver never sees a loop header. The
    ///     resolver can still probe it directly: its declaring hole is a BINDING
    ///     hole, so it recognises the new name without looking it up and resolves
    ///     against the enclosing scope the real path actually presents — one
    ///     where the loop variable is absent, because the loop declares it.
    ///     </para>
    ///     <para>
    ///     Spelled in the LEXER's words and not the reader's: «for each» is one
    ///     token, as «part of» is, so it is one segment. A pattern is matched
    ///     against lexemes, so its segments have to be things the lexer can
    ///     produce — «for» and «each» as two segments would never match anything.
    ///     </para>
    /// </remarks>
    internal static Pattern Previous { get; } = new([Injection.Shadow.Words[0], null]);

    public static IReadOnlyList<Pattern> Builtins { get; } =
    [
        new Pattern(["for each", null, "in", null], [1]),
        Previous,
    ];

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
    ///     Every entry must be one the lexer can actually produce. A SYMBOL
    ///     entry is bounded by <c>Symbol.Lex</c> advancing a single character, so
    ///     anything longer needs a <c>Symbol.Special</c> of its own — otherwise
    ///     the entry is dead and the statements using it silently fail to
    ///     resolve. A WORD entry has no such bound and needs none: «otherwise» is
    ///     one lexeme however long it is, because that is what a word is.
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
        foreach (var name in enclosing.reactives) reactives.Add(name);

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
    ///     Adds reactive names. The built-in «old (_)» accepts exactly these
    ///     after its hole has resolved to a bare name reference.
    /// </summary>
    public SymbolTable WithReactives(params string[] names)
    {
        foreach (var name in names)
        {
            Names.Add(name);
            reactives.Add(name);
        }

        return this;
    }

    /// <summary>
    ///     Declares constants, which are named but are not reactive.
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
        if (constants.Contains(cell))
            return $"no reading «{name}». «{cell}» is a constant, so it has no previous " +
                   $"value — use «{cell}».";

        if (Names.Contains(cell) && reactives.Contains(cell) is false)
            return $"no reading «{name}». «old (_)» takes a reactive name, and «{cell}» is not reactive.";

        return null;
    }

    private readonly HashSet<string> constants = [];
    private readonly HashSet<string> reactives = [];

    /// <summary>Whether a resolved name may fill the hole of «old (_)».</summary>
    internal bool IsReactive(string name) => reactives.Contains(name);

    /// <summary>
    ///     The built-in pattern's word and the runtime shadow prefix, both read
    ///     from the descriptor rather than spelled independently.
    /// </summary>
    ///
    /// <remarks>
    ///     Found by audit. These were a second, independent definition of the
    ///     same word. Changing <see cref="Injection.Shadow"/> must move both the
    ///     resolver pattern and the runtime node it allocates; one description
    ///     keeps those halves joined.
    /// </remarks>
    internal static string Old => Injection.Shadow.Words[0];
    internal static string Shadowed => Injection.Shadow.Prefix;

    public SymbolTable WithPatterns(params string[] patterns)
    {
        foreach (var pattern in patterns) Patterns.Add(Pattern.Parse(pattern));
        return this;
    }

}

internal readonly record struct Resolution(ResolutionKind Kind, int Cost, IReadOnlyCollection<string> Readings)
{
    /// <remarks>
    ///     The competing readings of a tie, which the diagnostic quotes and a
    ///     repair will one day offer as alternatives. Wrapped because every
    ///     construction below hands over a fresh array, and an array behind
    ///     «IReadOnlyCollection» still assigns through a cast — it reports
    ///     «IsReadOnly» as true while an element write succeeds, which is the
    ///     one shape a collection check has to be told about.
    /// </remarks>
    public IReadOnlyCollection<string> Readings { get; } = new ReadOnlyCollection<string>([.. Readings]);

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
