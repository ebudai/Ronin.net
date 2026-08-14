// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Ronin.Compiler;

/// <summary>
///     Finds every meaning a statement has, and ranks them by lookup count.
/// </summary>
///
/// <remarks>
///     <para>
///     Replaces <c>Identifier.Resolve</c>. That method received a single greedy
///     <c>Name</c> spanning every consecutive word, and tried to recover the real
///     boundaries afterwards by enumerating index permutations. This never
///     commits to a boundary at all: a phrase is a SPAN, and every span keeps
///     every way of reading it.
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
///     </para>
///     <para>
///     MORE THAN ONE READING IS THE ERROR, whatever they cost. Cost used to
///     decide, and a strictly cheaper reading won in silence — «send time to
///     live» simply meant the name, with nothing to report because nothing tied.
///     What cost does now is order the readings, so the likeliest is the first a
///     person sees. It may order the suggestions and it may never choose among
///     them: the moment it chooses, every silent capture this replaced comes
///     back looking like a feature.
///     </para>
///     <para>
///     The repair is to bracket, which promotes an argument to its own
///     substatement — and every reading has one, which is the property the whole
///     direction rests on and the reason two declaration rules survived the
///     others.
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

    public Resolver(SymbolTable symbols, int patternBindingPower = 7, SymbolKind kind = SymbolKind.Value)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        if (patternBindingPower < 0 || patternBindingPower > MaxBindingPower)
            throw new ArgumentOutOfRangeException(nameof(patternBindingPower));

        this.symbols = symbols;
        this.kind = kind;
        PatternBindingPower = patternBindingPower;

        // The operators this resolve admits. In value position, every operator
        // the language has; in type position, the type-mode table — the
        // function-type arrow today, «and»/«or» once algebra bases resolve. A
        // value cannot be added to a type and a type cannot be summed, so none of
        // the VALUE operators crosses over — the kinds are what keep «number +
        // text» from resolving to anything.
        operators = kind is SymbolKind.Value ? symbols.Operators : TypeOperators(PatternBindingPower);

        // The minimum binding powers the recurrences can ever ASK for, which is
        // a handful and not a range. A table indexed 0..31 spent five sixths of
        // itself on levels nothing queries — «E[i, j, 13]» is reachable only if
        // some operator binds at 13, and none does.
        //
        // Derived from the operator table rather than written down: an operator
        // added at a new level would otherwise index a slot that does not exist,
        // and the statements using it would silently fail to resolve.
        SortedSet<int> reachable = [0, PatternBindingPower];

        foreach (var op in operators.Values)
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
        foreach (var pattern in symbols.Applicable(kind))
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
        fillings = [];
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
        if (top.IsEmpty) return Resolution.NoParse;

        var readings = top.Alternatives;

        return top.Total > 1
             ? Resolution.Ambiguous(readings[0].Cost, readings, top.Total, top.Bounded)
             : Resolution.Resolved(readings[0].Cost, readings[0].Node);
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

        // A literal denotes a VALUE, so it is admitted where a value is wanted
        // and nowhere else: «var x => 3» is already refused at the parser as a
        // lone anonymous value, but «list of 3» reaches here, and offering the
        // literal would make it a type nothing checks. The kind is what says so.
        if (kind is SymbolKind.Value && j - i is 1 && lexemes[i].Kind is LexemeKind.Number)
            cell.Offer(0, new Node.Literal(lexemes[i].Text).At(Offset(lexemes, i), Length(lexemes, i, j)));

        if (CanName(lexemes, i, j))
        {
            var name = string.Join(' ', Enumerable.Range(i, j - i).Select(k => lexemes[k].Text));
            if (symbols.Mentionable(kind).Contains(name)) cell.Offer(1, new Node.Name(name).At(Offset(lexemes, i), Length(lexemes, i, j)));
        }

        // a bracketed substatement is one lookup however large it is, and it is
        // CLOSED, which is what lets «(compute total for a) + b» resolve
        if (j - i >= 2 && lexemes[i].Kind is LexemeKind.Open && lexemes[j - 1].Kind is LexemeKind.Close)
        {
            // A «[…]» is a list VALUE, inadmissible where a type is wanted, so it
            // is offered only in value position; a «(…)» is grouping, which a
            // bracketed type hole — «list of (number)» — needs in either kind.
            // Whether a «(…)» of several parts is a type is a question about the
            // constructor it fills, which is the type checker's and not the
            // resolver's — grouping is admitted, arity is deferred.
            var collection = lexemes[i].Text is "[";
            if (collection is false || kind is SymbolKind.Value)
                Group(lexemes, cell, i + 1, j - 1, collection);
        }

        // A language pattern with a stricter hole than an ordinary expression.
        // Offered as CLOSED: once the hole is known to be one bare reference,
        // its extent is fixed, so «old x + 1» can only be «(old x) + 1».
        // Treating it as an ordinary open-ended pattern would either swallow
        // «x + 1» (which is not a reference) or make «old x» unavailable as
        // the left operand at arithmetic binding power.
        //
        // Value only: «old (_)» is a reactive name's previous value, which is a
        // value and not a type. The reactive set it reads lives in the one table
        // this resolve shares, so the kind is what keeps «old count» out of a
        // type annotation rather than the table being a different one.
        if (kind is SymbolKind.Value) Previous(lexemes, cell, i, j);

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
            var filled = Match(pattern, 0, lexemes, i, j);

            foreach (var filling in filled.Tuples)
                target.Offer(1 + filling.Cost,
                             new Node.Call(pattern, filling.Arguments)
                                 .At(Offset(lexemes, i), Length(lexemes, i, j)),
                             filled.Bounded);

            // The fillings past the «Most» that were kept — a call is one lookup,
            // so the pattern contributes «filled.Total» readings and only its
            // cheapest few were offered.
            target.Beyond(filled.Total - filled.Tuples.Count, filled.Bounded);
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
        if (symbols.Names.ContainsKey(name) is false || symbols.IsReactive(name) is false) return;

        Node argument = new Node.Name(name);
        if (bracketed) argument = new Node.Group([new Node.Entry(null, argument)]);

        cell.Offer(bracketed ? 3 : 2, new Node.Previous(name, argument).At(Offset(lexemes, i), Length(lexemes, i, j)));
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
        // An empty COLLECTION is a value — the list with nothing in it. An empty
        // GROUP is brackets round no expression, which means nothing in a value
        // and IS something in a type: the nullary parameter list of a function
        // type, «() => number» from DELEGATES §1. So it is offered where a type is
        // wanted and refused where a value is, the same split «[…]» and «(…)»
        // already draw. The loop below reaches neither — it asks for an expression
        // between each pair of separators and an empty span has none.
        if (from == to)
        {
            if (collection) cell.Offer(1, new Node.Group([], Node.Grouping.List));
            else if (this.kind is SymbolKind.Type) cell.Offer(1, new Node.Group([], Node.Grouping.Group));

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

        List<(Cell Key, Cell Value)> divided = [];
        var keyed = 0;

        foreach (var end in separators.Append(to))
        {
            // A SECOND split, subordinate to the comma split: a collection's
            // entry is a value, or a key and a value divided by the «=» at its
            // own depth. One decision and no speculation, which is what keeps
            // the parse single — a key that is itself a collection carries its
            // own «=» below this depth, so "the one at depth zero" is exact
            // rather than a guess, and it is exact because «=» inside brackets
            // is only ever this.
            //
            // A TYPE round group splits the same way: «optional (a = b)» is a
            // keyed group that grouping admits and multiplicity refuses later, per
            // TYPEHALFDECISIONS §3 — so the «=» is kept as a key rather than left
            // inside a span no expression consumes, which is a no-reading that
            // reads to a caller as "«optional (a = b)» is not a type". A round
            // group is never a lookup value, so the kind below stays «Group».
            var associates = collection || this.kind is SymbolKind.Type
                           ? Associating(lexemes, start, end)
                           : [];

            // TWO in one entry is not an alternative to choose between; it is a
            // mistake, and the grammar has the message for it. Nothing here
            // refuses it, because nothing has to: an entry is split at ONE «=»,
            // so a second one is left inside a span asked to be an expression —
            // and no expression consumes an «Associates», which is what making it
            // a kind of its own guarantees. So the span has no derivation and the
            // whole group is refused, without a rule here that could disagree
            // with the one next door.
            Cell key = null;

            if (associates.Count is 1)
            {
                key = Expressions(start, associates[0], 0);

                if (key.IsEmpty) return;

                ++keyed;
                start = associates[0] + 1;
            }

            // an empty part — a leading or doubled separator — is not a
            // substatement at all
            var value = Expressions(start, end, 0);

            if (value.IsEmpty) return;

            divided.Add((key, value));
            start = end + 1;
        }

        // Part value and part association is neither a list nor a lookup. The
        // grammar says so with both positions named; here it is simply not a
        // derivation, and the same predicate decides both.
        if (Associated.Mixed(keyed, divided.Count)) return;

        // A round group is a group, unless a TYPE keyed it — «optional (a = b)» —
        // where the key must survive to the checker that will refuse it by
        // multiplicity. The node model carries a key only on a lookup, and in type
        // position that costs nothing: the tree is never evaluated, so it is never
        // a runtime lookup value, and «[…]» collections are suppressed in a type,
        // so nothing else here is a lookup to confuse it with.
        var kind = collection ? Associated.Kind(keyed)
                 : this.kind is SymbolKind.Type && keyed is not 0 ? Node.Grouping.Lookup
                 : Node.Grouping.Group;

        // EVERY part against every other, so a group carries the readings of
        // each of its parts rather than one tree and a count. A tie inside «(x,
        // y)» used to arrive at the group as "two derivations" with two
        // renderings of whichever part was ambiguous; it arrives as two
        // renderings of the GROUP now, which is what a person would bracket.
        //
        // A KEY is a cell like any other, so a lookup's readings are the product
        // over its keys AND its values — the same walk, over up to twice as many
        // cells.
        List<Cell> cells = [];

        foreach (var (key, value) in divided)
        {
            if (key is not null) cells.Add(key);

            cells.Add(value);
        }

        var readings = cells.Aggregate(1L, (product, part) => Cell.Saturating(product * part.Total));
        var bounded = cells.Any(part => part.Bounded);
        var built = 0;

        foreach (var combination in Combinations([.. cells.Select(part => part.Alternatives)]))
        {
            ++built;

            List<Node.Entry> parts = [];
            var taken = 0;

            foreach (var (key, _) in divided)
            {
                var chosen = key is null ? null : combination[taken++].Node;

                parts.Add(new Node.Entry(chosen, combination[taken++].Node));
            }

            cell.Offer(1 + combination.Sum(part => part.Cost), new Node.Group(parts, kind), bounded);
        }

        cell.Beyond(readings - built, bounded || readings > built);
    }

    /// <summary>
    ///     The association separators of one entry, at its own depth.
    /// </summary>
    ///
    /// <remarks>
    ///     A KIND rather than the text, so this cannot be broken by anything that
    ///     changes what «=» is elsewhere: it is <see cref="LexemeKind.Associates"/>
    ///     or it is not one.
    /// </remarks>
    private static List<int> Associating(IReadOnlyList<Lexeme> lexemes, int from, int to)
    {
        List<int> associates = [];
        var depth = 0;

        for (var k = from; k < to; ++k)
        {
            switch (lexemes[k].Kind)
            {
                case LexemeKind.Open: ++depth; break;
                case LexemeKind.Close: --depth; break;
                case LexemeKind.Associates when depth is 0: associates.Add(k); break;
                default: break;
            }
        }

        return associates;
    }

    /// <summary>
    ///     The cheapest ways of taking one derivation from each part.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     ONE combination where nothing is ambiguous, which is the case that
    ///     has to stay free: an unambiguous part contributes one alternative and
    ///     the product of ones is one, so an ordinary group yields a single
    ///     combination and allocates a single array.
    ///     </para>
    ///     <para>
    ///     CHEAPEST FIRST and BOUNDED, which the odometer that was here was
    ///     neither. Sixty-three independently ambiguous parts have 2^63
    ///     combinations, and a group of them is a maintained test — enumerating
    ///     them takes longer than the universe has. Cost is additive, so the
    ///     frontier walk below reaches the cheapest few without visiting the
    ///     rest: start at all-cheapest, and each step advances one part by one.
    ///     </para>
    /// </remarks>
    private static IEnumerable<IReadOnlyList<Best>> Combinations(IReadOnlyList<IReadOnlyList<Best>> parts)
    {
        PriorityQueue<int[], int> frontier = new();
        HashSet<string> seen = [];

        var first = new int[parts.Count];

        frontier.Enqueue(first, parts.Select((part, at) => part[0].Cost).Sum());
        seen.Add(string.Join(',', first));

        for (var taken = 0; taken < Cell.Most && frontier.Count is not 0; ++taken)
        {
            var at = frontier.Dequeue();

            yield return [.. at.Select((index, part) => parts[part][index])];

            for (var part = 0; part < parts.Count; ++part)
            {
                if (at[part] + 1 >= parts[part].Count) continue;

                var next = (int[])at.Clone();

                ++next[part];

                if (seen.Add(string.Join(',', next)) is false) continue;

                frontier.Enqueue(next, next.Select((index, which) => parts[which][index].Cost).Sum());
            }
        }
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
    ///     One way to fill a pattern's holes: the argument nodes, and their cost.
    /// </summary>
    ///
    /// <remarks>
    ///     A literal segment has to match but contributes no argument, which is
    ///     why the words do not appear here — <see cref="Node.Call"/> puts them
    ///     back by walking the same segments when it renders.
    /// </remarks>
    private readonly record struct Filling
    {
        public Filling(int cost, IReadOnlyList<Node> arguments)
        {
            Cost = cost;
            Arguments = Owned.Copy(arguments);
        }

        public int Cost { get; }

        /// <summary>The argument nodes, owned where the filling is made.</summary>
        public IReadOnlyList<Node> Arguments { get; }
    }

    /// <summary>
    ///     The cheapest ways to fill the rest of a pattern's holes over a span.
    /// </summary>
    ///
    private readonly record struct Fillings
    {
        public Fillings(IReadOnlyList<Filling> tuples, long total, bool bounded)
        {
            Tuples = Owned.Copy(tuples);
            Total = total;
            Bounded = bounded;
        }

        /// <summary>At most <see cref="Cell.Most"/>, cheapest first.</summary>
        public IReadOnlyList<Filling> Tuples { get; }

        /// <summary>How many fillings there are, saturated.</summary>
        public long Total { get; }

        /// <summary>Whether <see cref="Total"/> is a floor.</summary>
        public bool Bounded { get; }

        public static readonly Fillings None = new([], 0, false);
    }

    /// <summary>
    ///     Every way to match a pattern's holes against a span, cheapest first
    ///     and bounded.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     BOUNDED, which it was not, and that was a hang rather than a slow
    ///     path. The medial case combined every split point with every argument
    ///     and every completion and yielded them all — so a pattern of ten
    ///     adjacent holes over twenty words enumerated «C(19, 9)» fillings before
    ///     anything trimmed them, and a twenty-five-lexeme statement took twelve
    ///     seconds to resolve.
    ///     </para>
    ///     <para>
    ///     Cost is additive and never negative, so the cheapest few fillings of a
    ///     whole are built from the cheapest few of its parts — keeping <see
    ///     cref="Cell.Most"/> per subproblem is enough to reconstruct the
    ///     cheapest <see cref="Cell.Most"/> of the pattern. That is the same
    ///     property the cell relies on, applied to the recursion that feeds it,
    ///     and it turns the exponential enumeration polynomial.
    ///     </para>
    ///     <para>
    ///     MEMOISED by «(pattern, segment, position, end)» within one resolve,
    ///     because the same suffix over the same span is reached by every way of
    ///     splitting the holes before it. «end» belongs in the key: the table
    ///     fills «Atoms» for every span, so one «(pattern, segment, position)» is
    ///     asked for many ends, and a memo that dropped it would hand a filling
    ///     of one span back for another. The reads are all of strictly smaller
    ///     spans already final in the table, so a materialised result is stable.
    ///     </para>
    ///     <para>
    ///     The count is the sum over splits of the product of the parts' counts,
    ///     saturated — the honest number of fillings, not the number kept, so the
    ///     diagnostic can still say how many there are.
    ///     </para>
    /// </remarks>
    private Fillings Match(Pattern pattern, int segment, IReadOnlyList<Lexeme> lexemes, int position, int end)
    {
        if (fillings.TryGetValue((pattern, segment, position, end), out var memo)) return memo;

        return fillings[(pattern, segment, position, end)] = Matching(pattern, segment, lexemes, position, end);
    }

    private Fillings Matching(Pattern pattern, int segment, IReadOnlyList<Lexeme> lexemes, int position, int end)
    {
        if (segment == pattern.Segments.Count)
            return position == end ? new Fillings([new Filling(0, [])], 1, false) : Fillings.None;

        var word = pattern.Segments[segment];

        if (word is not null)
        {
            // A WORD or a SYMBOL, because a segment may be either: «lookup (_)
            // => (_)» joins its key to its value with an arrow, and resolving it
            // through anything but this matcher would be a second grammar with a
            // second ambiguity policy. Admitting the symbol here is what keeps
            // one resolver — and it is what makes «m => lookup text => number»
            // resolve uniquely, since the arrow takes part in ordinary resolution
            // under the kind filter.
            if (position < end
                && lexemes[position].Kind is LexemeKind.Word or LexemeKind.Symbol
                && lexemes[position].Text == word)
                return Match(pattern, segment + 1, lexemes, position + 1, end);

            return Fillings.None;
        }

        // A pinned hole is exactly one word, which is what makes the split
        // around it structural rather than scored: nothing can grow across it,
        // so the word after it needs no reserving.
        if (pattern.Pinned.Contains(segment))
        {
            // A BINDING occurrence, not a value. The hole declares the name, so
            // there is nothing to look up and nothing to score — resolving it as
            // an expression meant «for each bank in banks» only resolved when
            // «bank» was already declared, the one table the real path can never
            // present, and over-accepted «for each (a + b) in banks» besides.
            if (Binding(lexemes, position, end, out var only) is not Node.Binding name) return Fillings.None;

            if (segment == pattern.Segments.Count - 1)
                return only == end ? new Fillings([new Filling(0, [name])], 1, false) : Fillings.None;

            var after = Match(pattern, segment + 1, lexemes, only, end);

            // Prepending a binding costs nothing and adds nothing to count — the
            // name was determined, so the fillings after it are the fillings.
            return new Fillings([.. after.Tuples.Select(rest => new Filling(rest.Cost, [name, .. rest.Arguments]))],
                                after.Total,
                                after.Bounded);
        }

        if (segment == pattern.Segments.Count - 1)
        {
            // trailing argument: reaches the end of the span, parsed at the
            // pattern's own binding power
            var last = Expressions(position, end, PatternBindingPower);

            if (last.IsEmpty) return Fillings.None;

            return new Fillings([.. last.Alternatives.Select(trailing => new Filling(trailing.Cost, [trailing.Node]))],
                                last.Total,
                                last.Bounded);
        }

        List<Filling> combined = [];
        var total = 0L;

        for (var split = position + 1; split <= end; ++split)
        {
            // medial args cross any operator
            var medial = Expressions(position, split, 0);

            if (medial.IsEmpty) continue;

            var after = Match(pattern, segment + 1, lexemes, split, end);

            if (after.Tuples.Count is 0) continue;

            // EVERY argument against every completion — the cheapest «Most» of
            // each, which is enough for the cheapest «Most» of the pair. Taking
            // one tree here is what dropped a reading whose only alternative was
            // inside an argument the parent had already chosen for.
            foreach (var argument in medial.Alternatives)
            {
                foreach (var rest in after.Tuples)
                {
                    combined.Add(new Filling(argument.Cost + rest.Cost, [argument.Node, .. rest.Arguments]));
                }
            }

            total = Cell.Saturating(total + Cell.Saturating(medial.Total * after.Total));
        }

        var kept = combined.OrderBy(filling => filling.Cost).Take(Cell.Most).ToArray();

        // BOUNDED is exactly "were readings dropped, here or below". A part that
        // capped reports a saturated «Total» of at least «Most» + 1, which its
        // product carries into «total» — so «total > kept.Length» already covers
        // a bound below this level, and tracking each part's flag as well would
        // be testing a term that can never be the one that decides it.
        return new Fillings(kept, total, total > kept.Length);
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

            if (operators.TryGetValue(lexemes[k].Text, out var op) is false) continue;
            if (op.BindingPower < minimum) continue;

            // An operator is admitted where its binding power is at least the
            // minimum, so the side that may repeat it takes the operator's own
            // power and the side that may not takes one more. Left associative
            // therefore groups «a + b + c» as «(a + b) + c», and right
            // associative mirrors it. Handing both sides the higher minimum
            // forbids the operator on either, and a chain of one precedence
            // stops parsing altogether.
            //
            // NON-ASSOCIATIVE hands both sides the operator's OWN power, so each
            // grouping of «a op b op c» is admitted and the chain reads more than
            // one way — an ambiguity rather than a silent pick. That is the type
            // arrow: «a => b => c» has no natural grouping in an uncurried
            // language, so it is bracketed or it is a finding.
            var repeats = op.BindingPower;
            var excludes = op.BindingPower + 1;
            var leftminimum = op.Associativity is Associativity.Right ? excludes : repeats;
            var rightminimum = op.Associativity is Associativity.Left ? excludes : repeats;

            var before = Expressions(i, k, leftminimum);
            var after = Expressions(k + 1, j, rightminimum);

            if (before.IsEmpty || after.IsEmpty) continue;

            // EVERY operand against every other, and the count of what they
            // would have been. Taking one tree each side is what lost a reading
            // whose only alternative was inside an operand — «(sum of list) + x»
            // had one derivation at the top, because the operator combined two
            // operands and did not care that one of them was a tie.
            foreach (var left in before.Alternatives)
            {
                foreach (var right in after.Alternatives)
                {
                    cell.Offer(left.Cost + right.Cost,
                               new Node.Operation(left.Node, lexemes[k].Text, op, right.Node)
                                   .At(Offset(lexemes, i), Length(lexemes, i, j)),
                               before.Bounded || after.Bounded);
                }
            }

            cell.Beyond(Cell.Saturating(before.Total * after.Total)
                      - (before.Alternatives.Count * after.Alternatives.Count),
                        before.Bounded || after.Bounded);
        }
    }

    /// <summary>The source offset a lexeme range starts at.</summary>
    private static int Offset(IReadOnlyList<Lexeme> lexemes, int i) => lexemes[i].Offset;

    /// <summary>The source length a lexeme range covers.</summary>
    private static int Length(IReadOnlyList<Lexeme> lexemes, int i, int j)
        => lexemes[j - 1].Offset + lexemes[j - 1].Length - lexemes[i].Offset;

    /// <summary>
    ///     The cell for a span at a minimum binding power. Only the minima the
    ///     recurrences can ask for have a slot, so this maps one to the other.
    /// </summary>
    private Cell Expressions(int i, int j, int minimum) => expressions[(Span(i, j) * minima.Length) + slots[minimum]];

    /// <summary>How many readings of one statement are kept and offered as repairs.</summary>
    public const int Kept = Cell.Most;

    private Dictionary<string, List<Pattern>> anchored;
    private Dictionary<(Pattern, int, int, int), Fillings> fillings;
    private readonly int[] minima;
    private readonly int[] slots;
    private readonly SymbolTable symbols;

    /// <summary>Whether this resolve reads a value expression or a type annotation.</summary>
    private readonly SymbolKind kind;

    /// <summary>The operators this resolve admits, chosen by <see cref="kind"/>.</summary>
    private readonly IReadOnlyDictionary<string, Operator> operators;

    /// <summary>
    ///     The operators a type annotation admits.
    /// </summary>
    ///
    /// <remarks>
    ///     The function-type arrow today, and «and»/«or» once algebra bases
    ///     resolve. NON-ASSOCIATIVE, which is what makes «a => b => c» an ambiguity
    ///     rather than a silent grouping — an uncurried language has no reading to
    ///     prefer, so it is bracketed or it is a finding. Bound at
    ///     <see cref="PatternBindingPower"/>, the level a pattern's trailing
    ///     argument parses at, so a lookup type — itself a pattern call — may sit on
    ///     either side of an arrow; that is what gives «lookup text => number =>
    ///     truth» all three of its bracketings rather than two.
    /// </remarks>
    private static IReadOnlyDictionary<string, Operator> TypeOperators(int patternBindingPower)
        => new Dictionary<string, Operator>
        {
            [Lexicon.Arrow.symbol] = new Operator(patternBindingPower, Operator.Unevaluated, Associativity.None),
        };

    private int[] rows;
    private Cell[] closed;
    private Cell[] open;
    private Cell[] expressions;

    /// <summary>
    ///     Every derivation of one span, and what each costs.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     It used to hold one derivation, a count, and a WITNESS — two
    ///     renderings carried up from wherever the tie actually was, because a
    ///     parent kept one child tree and could not say what the others were.
    ///     That lost a reading whenever both halves were true at once: a span
    ///     with its own alternative, one of whose branches contained an
    ///     ambiguous child, reported its own two and dropped the child's
    ///     remaining one. Three readings, two offered, and the missing one was a
    ///     bracketing a person could have chosen.
    ///     </para>
    ///     <para>
    ///     A parent enumerates its children instead, so a cell holds every
    ///     reading of its own span and the witness has nothing left to do. There
    ///     is no longer a difference between where a tie IS and where it is
    ///     carried through, which is what the two shapes were for.
    ///     </para>
    ///     <para>
    ///     UNCAPPED here. The cap belongs to the diagnostic, which shows a few
    ///     and says how many there are — capping in the table would make the
    ///     total a guess at the one place it has to be a fact. Enumeration is
    ///     bounded by the ambiguity actually present: an unambiguous span has
    ///     one derivation and a parent combining two of them does one unit of
    ///     work, so a program with no ambiguity pays nothing for this.
    ///     </para>
    /// </remarks>
    private sealed class Cell
    {
        public int Cost { get; private set; } = int.MaxValue;

        public bool IsEmpty => order is null;

        /// <summary>
        ///     The cheapest derivations, in order, at most <see cref="Most"/>.
        /// </summary>
        ///
        /// <remarks>
        ///     CHEAPEST FIRST, because the diagnostic offers these in order and
        ///     the likeliest reading should be the first one a person sees. That
        ///     is the whole of what cost does now: it may order the suggestions
        ///     and it may never choose among them. The moment it chooses, every
        ///     silent capture this replaced comes back looking like a feature.
        /// </remarks>
        public Owned.Kept<Best> Alternatives
            => alternatives ??= Owned.Of(order.OrderBy(node => costs[node])
                                              .Take(Most)
                                              .Select(node => new Best(costs[node], node)));

        /// <summary>
        ///     How many derivations there are, which is not how many are kept.
        /// </summary>
        ///
        /// <remarks>
        ///     The diagnostic shows a few and says how many there are, so the
        ///     count has to outlive the cap. Saturating, because past a point the
        ///     only true thing to say is "more than anyone wants to read", and a
        ///     product across parts overflows long before that: a group of 63
        ///     independently ambiguous parts has 2^63 readings, which wraps
        ///     negative and is duly reported as unambiguous.
        /// </remarks>
        public long Total { get; private set; }

        /// <summary>
        ///     Whether derivations were dropped here or anywhere below.
        /// </summary>
        ///
        /// <remarks>
        ///     Cost is additive, so the cheapest few of a part are what the
        ///     cheapest few of the whole are built from — keeping <see
        ///     cref="Most"/> per span keeps the right ones. What it cannot keep
        ///     is an honest total, so this says when <see cref="Total"/> became a
        ///     floor rather than a count, and the diagnostic says "at least"
        ///     rather than quoting a number it made up.
        /// </remarks>
        public bool Bounded { get; private set; }

        public void Offer(int cost, Node node, bool bounded = false)
        {
            // Keyed by SHAPE and not by rendering. It was keyed by the
            // rendering, under a comment that made it a claim — two derivations
            // that read the same way ARE the same reading — and a nested call
            // renders without delimiting itself, so «print (send a to b)» and
            // «print (send a) to b» arrived here as the same string. The second
            // was dropped as a duplicate and the statement came back Resolved
            // with one meaning out of two.
            //
            // EVERY derivation is kept, not only the cheapest. Minimum lookup
            // used to discard the dearer ones here, which is what made a
            // strictly cheaper reading win in silence — «send time to live»
            // simply meant the name, and nothing said so.
            if (IsEmpty)
            {
                // On first offer, not at construction. Most cells in the table
                // are never offered anything — a span that is not an expression
                // still gets one per binding power — so eagerly allocating both
                // collections was two objects per cell for nothing.
                order = [];
                costs = new(Node.Same);
            }

            alternatives = null;
            Bounded |= bounded;

            if (cost < Cost) Cost = cost;

            if (costs.ContainsKey(node) is false)
            {
                order.Add(node);
                Total = Saturating(Total + 1);
                Bounded |= order.Count > Most;
            }

            // The CHEAPEST way to reach a shape, where two derivations arrive at
            // the same one: they are one reading, so they rank once and rank at
            // their best. Two identical declarations of a pattern are the
            // commonest way that happens, and collapsing them is overloading's
            // policy rather than this one's to undo.
            costs[node] = costs.TryGetValue(node, out var already) ? System.Math.Min(already, cost) : cost;
        }

        /// <summary>
        ///     The derivations a site knows about and did not enumerate.
        /// </summary>
        ///
        /// <remarks>
        ///     Counting the offers alone would say a group of 63 ambiguous parts
        ///     has five readings, which is the count of what fit rather than of
        ///     what there is. The product is known without building any of them,
        ///     so the total stays a fact and only the list is short.
        /// </remarks>
        public void Beyond(long extra, bool bounded)
        {
            Bounded |= bounded;

            if (extra > 0) Total = Saturating(Total + extra);
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
            foreach (var alternative in other.Alternatives) Offer(alternative.Cost, alternative.Node, other.Bounded);

            Beyond(other.Total - other.Alternatives.Count, other.Bounded);
        }

        // Dictionary is NOT insertion ordered in .NET, and the chosen reading must
        // be deterministic, so order is tracked explicitly alongside the costs.
        // Both are null until something is offered, which is what «IsEmpty» reads.
        private List<Node> order;

        /// <summary>What each derivation costs, for ordering them.</summary>
        private Dictionary<Node, int> costs;

        private Owned.Kept<Best> alternatives;

        /// <summary>How many derivations of one span are kept and offered as repairs.</summary>
        ///
        /// <remarks>
        ///     The same number the diagnostic shows, and that is not a
        ///     coincidence: keeping more than it shows would cost work at every
        ///     span to produce readings nobody sees, and keeping fewer would
        ///     leave it unable to fill its own list.
        /// </remarks>
        public const int Most = 5;

        /// <summary>Beyond this a total is only "more than anyone wants to read".</summary>
        public static long Saturating(long total) => total < Ceiling ? total : Ceiling;

        private const long Ceiling = 1000;
    }
}

/// <summary>One derivation of one span, and what it cost.</summary>
///
/// <remarks>
///     It carried a COUNT and a WITNESS too, because a parent kept one child
///     tree and needed some way to say that the child had others. A parent
///     enumerates its children now, so the alternatives are the alternatives and
///     there is nothing to carry beside them — which also ends the distinction
///     between a tie a cell can see and a tie it was only told about.
/// </remarks>
internal readonly record struct Best(int Cost, Node Node);

internal enum LexemeKind { Word, Number, Symbol, Open, Close, Separator, Associates }

/// <param name="Announces">
///     Whether this word is a keyword that introduces a production, which is the
///     one thing a name may not BEGIN with. Carried rather than re-derived,
///     because the resolver has no tokens left to ask — and a keyword is an
///     ordinary word everywhere else, so it cannot be a <see cref="LexemeKind"/>
///     of its own without taking «var ready if needed» out of the language.
/// </param>
/// <param name="Text">
///     The CANONICAL spelling, which is not always the source slice: «for  each»
///     is the same keyword as «for each» and has to be the same lexeme.
/// </param>
///
/// <param name="Offset">Where it starts in the source it was lexed from.</param>
///
/// <param name="Length">How long it is THERE, which the canonical text cannot say.</param>
///
/// <remarks>
///     The position is carried because <see cref="Text"/> cannot answer for it.
///     A repair is an EDIT — an editor needs somewhere to put a bracket, not a
///     sentence describing where one would go — and a name is a run of words, so
///     colouring one as a unit needs to know where the run starts and stops. Both
///     were unanswerable from a lexeme, and both were sitting on the token it was
///     built from.
/// </remarks>
internal readonly record struct Lexeme(LexemeKind Kind, string Text, bool Announces = false, int Offset = 0, int Length = 0)
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

            // A SYMBOL may be a segment, which is what lets «lookup (_) => (_)»
            // be an ordinary pattern rather than a second grammar. It reserves
            // NOTHING: glue exists because a name can swallow a word between two
            // holes, and a name cannot swallow a symbol — the lexer stops a word
            // at one — so R5′, R6b and R7b are about words and a symbol segment
            // is invisible to them.
            //
            // The restriction that it be a symbol the lexer already produces
            // needs no code: this reads back what the lexer made, so punctuation
            // a pattern author assembled — «~>» where the lexer yields «~» then
            // «>» — arrives as two segments and fails to read back as itself.
            if (lexemes[at].Kind is LexemeKind.Symbol) { segments.Add(lexemes[at].Text); continue; }

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

                // A SYMBOL segment reserves nothing, so it is not glue. Glue
                // exists because a name can swallow a word that sits between two
                // holes; a name cannot swallow a symbol, because the lexer stops
                // a word at one. So «lookup (_) => (_)» takes no word out of
                // anybody's vocabulary, and the name rules — which are about
                // words — never see it.
                if (Segments[segment].Any(letter => char.IsLetter(letter) is false)) continue;

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
///     Which way a chain of one operator groups.
/// </summary>
///
/// <remarks>
///     <see cref="None"/> is not a fourth thing bolted on: an operator groups
///     left, groups right, or does not group, and the third is what makes a chain
///     of it an ambiguity the reader must bracket. The type arrow is the first to
///     want it — «a =&gt; b =&gt; c» has no grouping to prefer where nothing is
///     curried, so preferring one would be the silent pick this language refuses.
/// </remarks>
internal enum Associativity { Left, Right, None }

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

    public Operator(int bindingPower, Func<object, object, object> apply,
                    Associativity associativity = Associativity.Left)
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
        Associativity = associativity;
    }

    public int BindingPower { get; }

    /// <summary>What the operator does. Required, so resolution and evaluation cannot disagree about whether it has a meaning.</summary>
    public Func<object, object, object> Apply { get; }

    /// <summary>
    ///     Which way a chain of this operator groups, or that it does not group at
    ///     all — in which case a chain of it is an ambiguity rather than a pick.
    /// </summary>
    public Associativity Associativity { get; }

    /// <summary>
    ///     The meaning of an operator that resolves but is never evaluated.
    /// </summary>
    ///
    /// <remarks>
    ///     The type arrow is the first: it groups the key and value of a function
    ///     type, and a type is not a value, so no tree it sits in is ever handed to
    ///     the evaluator. <see cref="Apply"/> is required all the same — an operator
    ///     without a meaning is the bug the constructor guards — so this is the
    ///     meaning a resolution-only operator carries: reaching it means a type
    ///     tree escaped into evaluation, which is a defect and says so rather than
    ///     returning a wrong value quietly.
    /// </remarks>
    public static object Unevaluated(object left, object right)
        => throw new System.InvalidOperationException("a type does not evaluate to a value");
}

/// <summary>
///     What an entry in the symbol table names.
/// </summary>
///
/// <remarks>
///     ONE TABLE, entries carrying a kind, rather than a table per kind. A second
///     table depends on the position selecting it, and «type of x» is the case
///     that cannot answer: it puts a type where a value goes. Separated, every
///     name rule — R5′, R6b, R7b, self-ambiguity — would run twice and have to be
///     kept in step, and the failure when it is not is silent, which is the most
///     expensive kind this project has had. The same move as carrying a
///     distinction as a field rather than forking the derivations that encode it.
/// </remarks>
internal enum SymbolKind
{
    /// <summary>What a reference in an expression may resolve to.</summary>
    Value,

    /// <summary>What a reference in an annotation may resolve to.</summary>
    Type,
}

/// <summary>Names and patterns in scope, plus the fixed operator table.</summary>
internal sealed class SymbolTable
{
    public Dictionary<string, SymbolKind> Names { get; } = [];

    public List<(Pattern Pattern, SymbolKind Kind)> Patterns { get; } = [];

    /// <summary>
    ///     Every pattern a call in this scope may resolve against.
    /// </summary>
    ///
    /// <remarks>
    ///     What was DECLARED, plus the builtins that are ordinary patterns —
    ///     which today is «return (_)» alone. The other two are in
    ///     <see cref="Builtins"/> for their shapes rather than as calls: a loop
    ///     header never reaches the resolver, and «old (_)» has a hole no
    ///     expression may fill, so each is probed directly.
    ///     <para>
    ///     Kept apart from <see cref="Patterns"/> rather than seeded into it,
    ///     because "what did this scope declare" is a question several things
    ///     ask and a builtin is not an answer to it. A pattern nobody wrote
    ///     showing up in a scope's declarations is the same confusion as a
    ///     generated name showing up among written ones.
    ///     </para>
    /// </remarks>
    public IEnumerable<Pattern> Callable
        => Patterns.Where(entry => entry.Kind is SymbolKind.Value)
                   .Select(entry => entry.Pattern)
                   .Append(Answer).Append(Exit).Append(Halt);


    /// <summary>
    ///     Every name a reference in this scope may resolve to.
    /// </summary>
    ///
    /// <remarks>
    ///     What was DECLARED plus what the language supplies, kept apart for the
    ///     reason <see cref="Callable"/> is: "what did this scope declare" is a
    ///     question several things ask, and a word nobody wrote is not an answer
    ///     to it.
    /// </remarks>
    public IEnumerable<string> Known
        => Names.Where(entry => entry.Value is SymbolKind.Value).Select(entry => entry.Key).Concat(Truths);

    /// <summary>
    ///     Every type a reference in an annotation may resolve to.
    /// </summary>
    ///
    /// <remarks>
    ///     <see cref="Known"/> read the other way — the same table, the type kind
    ///     rather than the value kind, and the type spellings the language
    ///     supplies in place of the truths. A type name IS a name; the kind is what
    ///     tells «number» the type from a variable someone called it, and this is
    ///     one table filtered rather than a second one.
    /// </remarks>
    public IEnumerable<string> TypeNames
        => Names.Where(entry => entry.Value is SymbolKind.Type).Select(entry => entry.Key).Concat(SuppliedTypes);

    /// <summary>
    ///     Every type pattern a reference in an annotation may resolve against —
    ///     the type constructors the language supplies.
    /// </summary>
    ///
    /// <remarks>
    ///     <see cref="Callable"/> read the other way, less the declared half —
    ///     because there is no declared half yet. A pattern declaration records
    ///     the VALUE kind whatever keyword introduces it, so <see cref="Patterns"/>
    ///     holds nothing of the type kind to select, and filtering it would be
    ///     testing a branch nothing can reach. The declared half joins here when a
    ///     parameterised type can be written — exactly as <see cref="TypeNames"/>
    ///     already carries a «type X;» — and FIVE-RULINGS §4 is why it will be one
    ///     registry then rather than two: a type constructor and a value pattern of
    ///     one shape are two candidates told apart by what they return.
    /// </remarks>
    public IEnumerable<Pattern> TypePatterns => SuppliedTypePatterns;

    /// <summary>
    ///     The names a reference of the given kind may mention — <see cref="Known"/>
    ///     for a value, <see cref="TypeNames"/> for a type.
    /// </summary>
    ///
    /// <remarks>
    ///     One filter read according to position. A value position admits the value
    ///     kind and a type annotation admits the type kind, and the resolver asks
    ///     for whichever the reference it is reading wants — the kind is a predicate
    ///     over the one table, not a choice between two of them.
    /// </remarks>
    public IEnumerable<string> Mentionable(SymbolKind kind)
        => kind is SymbolKind.Value ? Known : TypeNames;

    /// <summary>
    ///     The patterns a reference of the given kind may call — <see cref="Callable"/>
    ///     for a value, <see cref="TypePatterns"/> for a type.
    /// </summary>
    public IEnumerable<Pattern> Applicable(SymbolKind kind)
        => kind is SymbolKind.Value ? Callable : TypePatterns;

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

    /// <summary>
    ///     «return (_)» — a pattern, and deliberately not a keyword.
    /// </summary>
    ///
    /// <remarks>
    ///     A word that parses must live in the table the name rules run over,
    ///     because a keyword is a name those rules cannot see. As a keyword,
    ///     «return value» stays declarable — and it then WINS, at one lookup
    ///     against the call's two, silently. As a pattern the same rule that
    ///     refuses every other capture refuses it, with the message it already
    ///     produces.
    ///     <para>
    ///     It costs what an anchor-only pattern costs: no name may begin
    ///     «return». Measured at 0.058% of a large corpus, which is less than
    ///     half of what «old» was already taken for.
    ///     </para>
    /// </remarks>
    internal static Pattern Answer { get; } = new(["return", null]);

    /// <summary>
    ///     «optional (_)» — a type constructor, and no longer a modifier keyword.
    /// </summary>
    ///
    /// <remarks>
    ///     By the same law as <see cref="Answer"/>: a modifier keyword is a word
    ///     that parses and is not in the table the name rules run over, so
    ///     «optional value» stayed declarable and captured. It is also the last
    ///     type constructor that was not a pattern — every other one already is
    ///     — so leaving it a keyword was the fork rather than the change.
    ///     <para>
    ///     Reserved but not CALLABLE, like the loop header and «old (_)»: it
    ///     belongs in type position, and type position resolves against a table
    ///     that does not exist yet. Reserving the word now is what stops a name
    ///     taking it before that table arrives, which is the whole cost and the
    ///     whole point.
    ///     </para>
    ///     <para>
    ///     0.017% — 53 names in a large corpus, the cheapest reservation taken.
    ///     </para>
    /// </remarks>
    internal static Pattern Optional { get; } = new(["optional", null]);

    /// <summary>«list of (_)», whose «of» reads correctly for one parameter.</summary>
    internal static Pattern Listing { get; } = new(["list", "of", null]);

    /// <summary>
    ///     «lookup (_) => (_)», whose arrow reads correctly for a mapping where
    ///     «of» reads correctly for one parameter.
    /// </summary>
    internal static Pattern Lookups { get; } = new(["lookup", null, Lexicon.Arrow.symbol, null]);

    /// <summary>
    ///     «return» with nothing after it — leaving a body that has no answer.
    /// </summary>
    ///
    /// <remarks>
    ///     One concept at two arities rather than two operations that collided
    ///     on a word: both mean «leave this body now», and they differ in
    ///     whether there is an answer to carry. The runtime has had this one
    ///     since before either was spelled.
    ///     <para>
    ///     Prefix-related to <see cref="Answer"/> and not ambiguous with it,
    ///     which the deleted prefix-free clause is what permits: there is no
    ///     juxtaposition, so «return» followed by a word is not a composition of
    ///     the nullary form with anything. It is not a reading at all, so «return
    ///     x» can only be the one-hole pattern and «return» alone only this.
    ///     </para>
    /// </remarks>
    internal static Pattern Exit { get; } = new(["return"]);

    /// <summary>
    ///     «stop» — removes this «when», so it does not fire again.
    /// </summary>
    ///
    /// <remarks>
    ///     Nullary, like bare <see cref="Exit"/>, and reserved GLOBALLY rather
    ///     than inside a «when». Scoping the reservation is tempting and is
    ///     wrong for a reason already paid for: the self-ambiguity check is
    ///     deliberately pessimistic so that it is order-independent, and a
    ///     reservation that depends on where you are standing gives that check
    ///     two answers for one span — and lets a name declared outside a «when»
    ///     be captured inside one.
    ///     <para>
    ///     The reservation and the legality are separate, as they are for
    ///     «return (_)». That is what buys a five-name cost AND a good message,
    ///     rather than choosing between them.
    ///     </para>
    /// </remarks>
    internal static Pattern Halt { get; } = new(["stop"]);

    /// <summary>
    ///     Everything the language supplies, described where it is defined.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE LIST, from which the patterns and the names are derived rather
    ///     than kept beside it. A pattern and a literal were an
    ///     «IReadOnlyList&lt;Pattern&gt;» beside an «IReadOnlyList&lt;string&gt;»,
    ///     which is the fragmentation the one-table ruling is against showing up
    ///     in a second place.
    /// </remarks>
    public static IReadOnlyList<Descriptor> Supplies { get; } =
    [
        Descriptor.Shaped("Runs its body once for each element, binding the element to a name.",
                          new Pattern(["for each", null, "in", null], [1]))
            with { Forms = ["for each «one name» in (the list)"] },

        Descriptor.Shaped("The value a reactive name held before this step.", Previous)
            with
            {
                Forms = ["old (a reactive name)"],
                Legal = "Its argument must be a bare reactive name — «old (x + 1)» is not a previous value of anything.",
            },

        Descriptor.Shaped("Ends the current body, carrying an answer out of it.", Answer)
            with
            {
                Forms = ["return (the answer)"],
                Legal = "A function that answers. An action and a «when» body have nothing to answer, "
                      + "so they take the form with no argument.",
                SeeAlso = ["return"],
            },

        Descriptor.Shaped("Ends the current body without an answer.", Exit)
            with
            {
                Forms = ["return"],
                Legal = "An action, or a «when» body — where it ends the current firing and leaves the "
                      + "«when» in place. A body answers or it does not, and mixing the two forms is refused.",
                SeeAlso = ["return (_)"],
            },

        Descriptor.Shaped("Removes this «when», so it does not fire again and stops costing anything.", Halt)
            with
            {
                Forms = ["stop"],
                Legal = "Only inside a «when» body. To end the current firing and leave the «when» in "
                      + "place, write «return».",
                SeeAlso = ["return"],
            },

        Descriptor.Shaped("A type whose value may be absent.", Optional)
            with { Forms = ["optional (a type)"], Kind = SymbolKind.Type },

        Descriptor.Shaped("A type whose values are lists of one element type.", Listing)
            with { Forms = ["list of (a type)"], Kind = SymbolKind.Type },

        Descriptor.Shaped("A type whose values map keys of one type to values of another.", Lookups)
            with
            {
                Forms = ["lookup (a key type) => (a value type)"],
                Kind = SymbolKind.Type,
                Legal = "The arrow reads correctly for a mapping where «of» reads correctly for one "
                      + "parameter, which is why this and «list of (_)» are spelled differently.",
            },

        Descriptor.Spelled("Truth.", "true") with { SeeAlso = ["false"] },
        Descriptor.Spelled("Untruth.", "false") with { SeeAlso = ["true"] },

        Descriptor.Spelled("The type of numbers.", "number") with { Kind = SymbolKind.Type },
        Descriptor.Spelled("The type of text.", "text") with { Kind = SymbolKind.Type },
        Descriptor.Spelled("The type of «true» and «false».", "truth")
            with { Kind = SymbolKind.Type, SeeAlso = ["true", "false"] },
        Descriptor.Spelled("The type of a failure, which every type admits and which admits nothing.", "error")
            with { Kind = SymbolKind.Type },
    ];

    /// <summary>
    ///     Spellings the language supplies whole, which nobody may declare.
    /// </summary>
    ///
    /// <remarks>
    ///     A NULLARY entry reserves its own spelling and nothing else — that is
    ///     what makes «true positive» and «stop word» legal beside «true» and
    ///     «stop». What it does not do is reserve nothing at all, which is what
    ///     it did: «var return» was declarable, and then every bare «return» in
    ///     scope read two ways with no bracket able to separate them. The prefix
    ///     reservation «return (_)» pays for does not reach the whole name,
    ///     because a name equal to a pattern's words is deliberately left alone
    ///     — an argument cannot sit beside it.
    ///     <para>
    ///     Nullary is exactly where that reasoning stops holding: with no hole
    ///     there is no argument to sit anywhere, so the name and the call cover
    ///     the same span and neither is reachable past the other.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<string> Whole { get; }
        = [.. Supplies.Where(supplied => supplied.Shape?.Segments.Any(segment => segment is null) is not true)
                      .Select(supplied => supplied.Name)
                      .Order(System.StringComparer.Ordinal)];

    /// <summary>
    ///     The two truths, which the language supplies rather than anyone
    ///     declaring.
    /// </summary>
    ///
    /// <remarks>
    ///     A literal is a NULLARY entry — a name, not a pattern — so each
    ///     reserves its own spelling and nothing else. «true positive» and
    ///     «truth table» stay legal, which they would not if these were
    ///     anchor-only patterns.
    ///     <para>
    ///     They arrive with the type rather than after it: a fixture cannot
    ///     declare something a truth and then initialise it without one, so a
    ///     «truth» whose literals were deferred would be a type nothing could
    ///     test.
    ///     </para>
    /// </remarks>
    public static IReadOnlyList<string> Truths { get; }
        = [.. Supplies.Where(supplied => supplied.Shape is null && supplied.Kind is SymbolKind.Value)
                      .Select(supplied => supplied.Name)
                      .Order(System.StringComparer.Ordinal)];

    /// <summary>
    ///     The type spellings the language supplies whole, which every scope names
    ///     without declaring — «number», «text», «truth», «error».
    /// </summary>
    ///
    /// <remarks>
    ///     The type-kind counterpart of <see cref="Truths"/>, from the same list by
    ///     the same shape: a supplied entry with no pattern and the type kind.
    ///     «truth» is here as the type; its literals «true» and «false» are in
    ///     <see cref="Truths"/> as values — one word, two kinds, told apart by the
    ///     field the whole design turns on.
    /// </remarks>
    public static IReadOnlyList<string> SuppliedTypes { get; }
        = [.. Supplies.Where(supplied => supplied.Shape is null && supplied.Kind is SymbolKind.Type)
                      .Select(supplied => supplied.Name)
                      .Order(System.StringComparer.Ordinal)];

    /// <summary>The supplied patterns, for the rules that ask what words are taken.</summary>
    public static IReadOnlyList<Pattern> Builtins { get; }
        = [.. Supplies.Where(supplied => supplied.Shape is not null).Select(supplied => supplied.Shape)];

    /// <summary>
    ///     The type constructors the language supplies — «list of (_)»,
    ///     «optional (_)», «lookup (_) => (_)».
    /// </summary>
    ///
    /// <remarks>
    ///     A subset of <see cref="Builtins"/>: the supplied patterns of the type
    ///     kind. <see cref="Builtins"/> is every supplied shape, for the rules that
    ///     ask what words are taken; this is the ones a type annotation may call,
    ///     which is <see cref="Callable"/>'s question read of the type kind.
    /// </remarks>
    public static IReadOnlyList<Pattern> SuppliedTypePatterns { get; }
        = [.. Supplies.Where(supplied => supplied.Shape is not null && supplied.Kind is SymbolKind.Type)
                      .Select(supplied => supplied.Shape)];


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
        foreach (var (name, kind) in enclosing.Names) Names[name] = kind;
        foreach (var name in enclosing.constants) constants.Add(name);
        foreach (var name in enclosing.reactives) reactives.Add(name);

        Patterns.AddRange(enclosing.Patterns);

        return this;
    }

    /// <summary>The scope as it is, without the injection a declaration performs.</summary>
    public SymbolTable WithNames(params string[] names) => WithNames(SymbolKind.Value, names);

    /// <summary>The same, for names of a kind other than the ordinary one.</summary>
    public SymbolTable WithNames(SymbolKind kind, params string[] names)
    {
        foreach (var name in names) Names[name] = kind;
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
            Names[name] = SymbolKind.Value;
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
            Names[name] = SymbolKind.Value;
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

        if (Names.ContainsKey(cell) && reactives.Contains(cell) is false)
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
        foreach (var pattern in patterns) Patterns.Add((Pattern.Parse(pattern), SymbolKind.Value));
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

    /// <param name="total">
    ///     How many readings there are, which is not how many are shown.
    /// </param>
    ///
    /// <param name="bounded">
    ///     Whether <paramref name="total"/> is a floor rather than a count,
    ///     because a span somewhere had more derivations than were kept. A
    ///     message quoting a number it made up is worse than one saying "at
    ///     least" — and a cap nobody is told about reads as "these are all of
    ///     them", which is the shape of every silent thing this design removes.
    /// </param>
    public static Resolution Ambiguous(int cost, IReadOnlyList<Best> readings, long total, bool bounded)
        => new(ResolutionKind.Ambiguous, cost, [.. readings.Select(reading => reading.Node.ToString())])
        {
            Total = total,
            Bounded = bounded,
            Alternatives = Owned.Of(readings.Select(reading => reading.Node)),
        };

    /// <summary>
    ///     The competing readings as TREES, which is what they are.
    /// </summary>
    ///
    /// <remarks>
    ///     The resolver was taught to keep alternatives apart by shape rather
    ///     than by how they read, and then handed them over as renderings — so
    ///     everything downstream inherited exactly the non-injectivity that was
    ///     removed. Two different calls spanning the same words print the same
    ///     sentence, and a repair searching for that sentence found the same
    ///     bracket twice and offered one meaning as though it were two.
    ///     <para>
    ///     Rendering is a presentation of a tree and belongs at the boundary
    ///     where something is presented. This is the boundary where something is
    ///     REPAIRED, and a repair is about the tree.
    ///     </para>
    /// </remarks>
    public IReadOnlyList<Node> Alternatives { get; private init; } = Owned.Copy<Node>([]);

    /// <summary>How many readings the statement has, of which <see cref="Readings"/> shows a few.</summary>
    public long Total { get; private init; } = 1;

    /// <summary>Whether <see cref="Total"/> is a floor rather than a count.</summary>
    public bool Bounded { get; private init; }

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
