# Notes from reading the source

Read `resolver-and-symbol-separation` at `442268f`, 421 tests. Everything below
is from the code, not from the design documents — which matters, because most
of what I had written before reading it was either already fixed or wrong.

I wrote six items against `9c5d63f`. Four of them you had already closed by the
time I finished. What follows is what survived, plus verification of two things
I had filed as defects and you have since rewritten.

---

## STILL LIVE

### 1. No cutoff on recompute

`Compiler/Runtime/Graph.cs` — `Propagate` has the equal-value check with its
comment; `Recompute` has no counterpart. It sets `node.Value` and clears
`Dirty` with no comparison, so a `let` that recomputes to an unchanged value
leaves its dependents dirty and the wave runs on.

Measured on the reference, a coarse value derived from a fine one:

```
59 one-pixel mouse moves; the derived value changes twice
  without cutoff:  177 downstream recomputes
  with cutoff:       6      → 97% wasted
```

This is *cutoff* in the incremental-computation literature — one of the harder
lessons in Jane Street's seven rewrites of `Incremental`.

Two caveats. Equality must be the language's, and for an array-valued cell that
is O(n) — cutoff can cost more than the recompute it saves, so cut off scalars
unconditionally and use a digest or skip for arrays.

And it matters more here than in most reactive systems: **`old` shadows advance
every step.** Without cutoff, a shadow copying an unchanged value wakes its
dependents every tick, forever. The graph never goes quiet even when nothing is
happening.

### 2. Diagnostics want labelled spans, plural

`Token.Append` now carries real offsets (`25fe384`), so the blocker is gone.
Before the diagnostic type gets designed, though: **every diagnostic in this
design names two or more things.**

- a tie names both readings
- R5 names the name and the pattern
- multi-writer names both `when`s
- a cascade ring names the whole ring
- overload ambiguity names every candidate

A single `Location` field will be wrong on day one. It wants a primary span
plus a list of `(span, label)` — the Rust/Elm shape. Cheap now, invasive later,
and this language leans on its messages more than most.

---

## VERIFIED, NOT ACTION

### `Numeric.Lex` — the defect I filed is gone, and the replacement is correct

I filed a live defect against the old version: both regexes were unanchored
while `AdvanceBy(match.Length)` ignored `match.Index`, so `1234,567` consumed
`1234,56` — the token text and the text the regex approved were different
strings, silently.

The hand-scanner replaces it entirely, so the class of bug is gone with the
regex. I transcribed `Digits`/`Grouped` and ran them rather than eyeballing:

```
source          token          decimal   rest
1,234           1,234          False     ''
1,23            1              False     ',23'
1,2345          1              False     ',2345'
12,345,678      12,345,678     False     ''
1,234,56        1,234          False     ',56'
1,234.567890    1,234.567890   True      ''
12,3456,789     12             False     ',3456,789'
1,              1              False     ','
1234,567        1234           False     ',567'
```

Correct on all of them, including the two that were wrong before. The shrink
loop terminates for the reason the comment gives — a comma-free run is always
well formed — and I checked that `Grouped` returning false guarantees a comma
exists for the inner scan to find, so it cannot underflow.

`IsDecimal` being lexical is also exactly right for the numeric tower: integer
literals become int64, decimal literals become scaled integers, and the lexer
is the only place that distinction is free.

### `Datum.Parameter(declaring: false)` — the right shape, and better than I proposed

`declaring && type is null` is precisely the split. I had told you the guard
protected assignments from becoming declarations; that was wrong — Association
precedes Member in `Statement.Parse`, so `order = 3` never reaches Datum in
statement position at all. The clause that actually earns its keep is the
bare-name one, since `Member.Parse` precedes `Value.Parse` and a relaxed Datum
would claim a bare reference as a declaration.

Your fix is right either way, and does not depend on my reasoning being right.

### `Literal.Parse` — one token

`TryAdvance` is correct. Worth recording *why* the old code looked justified:
the class comment shows `12:33p` and `$75`, which would be multi-token. They do
not exist — `Lexicon.Literal` is `Date | Numeric | Text`, each one token, and
`Date` handles only `YYYY-MM-DD`. When those literals do arrive, lex them as
single tokens the way `Date` does rather than reviving parse-level gluing.

---

## On weighting my claims

Of the six items I wrote against `9c5d63f`: two were already fixed before I
read the code, two more were fixed while I was writing, one was a real defect
you had independently rewritten away, and one of my *explanations* was wrong
even where the conclusion held.

Design-level reasoning in this project has held up. **Claims about this
codebase have not.** I will read the source first from here. Until then,
verify anything I say about a specific file before acting on it — and the
inverse is also worth knowing: when I say a rule is unsound or a semantics
question has a particular answer, that reasoning has survived scrutiny much
better.
