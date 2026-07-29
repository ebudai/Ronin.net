# Sixth re-evaluation — pinning, canonical identity, and the two sweep items

Audited at commit `aa00249` (`Close the two sweep items`).

This review treated `0f87712..aa00249` as both a verification of every
`REAUDIT5` repair and a fresh audit of the new resolver, renderer, pattern, and
statement-boundary work. It also followed the two decisions in `SWEEPITEMS.md`.

Sign-off is withheld. The direct repairs for all eight `REAUDIT5` findings
hold, and the refined pin still justifies `RESERVED (0)`. The new audit found
one source-reachable identity bug, one unresolved binding-hole integration
problem, three internal invariant/diagnostic defects, and remaining live
documentation drift.

## 1. Multi-word keyword identity is canonical only on the resolver side

**Severity: high — source-reachable duplicate declarations and dead patterns**

`Keyword.Canonical` correctly normalizes `part  of`, `part\tof`, and
`for  each` when tokens become resolver lexemes. Declarations take a different
path:

```csharp
public string Words
    => string.Join(' ', Tokens.ToArray().Select(token => token.Memory.ToString()));
```

`Name.Words` retains the source slice verbatim. `Identifier.TryPattern` then
loses token identity a second time:

```csharp
segments.AddRange(name.Words.Split(' '));
```

That creates two independently observable failures.

### The same name can be declared more than once

This file exits zero with no findings:

```ronin
var ready part of world => Number;
var ready part  of world => Number;
var ready for each value => Number;
var ready for	each value => Number;
```

Observed CLI summary:

```text
4 statement(s), 8 name(s), 0 pattern(s)
1 file(s), 0 with problems
```

Each pair is the same token sequence according to `Keyword.Spaced` and
`Keyword.Canonical`, but the declaration table stores two different raw
strings. Shadowing therefore misses both duplicates. Resolution canonicalizes
either spelling back to the single-space form, so the noncanonical declaration
is unreachable.

### A source-declared pattern containing a multi-word keyword is dead

For example:

```ronin
function compute part of (x => Number) { return x; }
```

The lexer produces the lexeme sequence:

```text
compute | part of | 1
```

The declaration builder records pattern segments:

```text
compute | part | of | hole
```

Consequently `compute part of 1` is `NoParse` against the symbol table built
from that source. With doubled spaces, `Split(' ')` also inserts an empty
literal segment, making a second dead shape.

This is the newest instance of hand-built data standing in for the real path:
a token sequence is rendered to a whitespace string and then split as though
spaces still encoded token boundaries. It also affects `Completion`, which
splits canonical name strings while the typed lexemes retain `part of` as one
element.

**Recommendation:** make the canonical sequence of word-token identities a
first-class value on `Name`. Build declaration keys, pattern segments,
completion candidates, and diagnostics from that sequence. Render it to a
string only at presentation/key boundaries; never render and then `Split`.
Add source-to-declarations-to-resolver tests for multi-word keywords in both
names and pattern anchors, over the full whitespace matrix, plus duplicate
declaration tests across equivalent spacing.

## 2. The loop pin is an expression hole, not the settled binding hole

**Severity: high integration blocker, currently masked by the resolver not
being joined to `Compilation`**

The refined pin enforces extent correctly: it consumes one lexeme or one
balanced-looking bracketed extent. That is enough to prevent a name growing
across `in`, so the zero-reservation argument survives.

It does not enforce the other half of the settled design. `SWEEPITEMS.md`
states that the loop hole is a binding occurrence rather than a value. In
`Resolver.Match`, a pinned hole is still resolved through `Expressions`:

```csharp
if (Expressions(position, split, 0).TryBest(out var argument) is false)
    continue;
```

Therefore `for each bank in banks` resolves only if `bank` is already in
`SymbolTable.Names`. Every loop-resolution test supplies the new variable by
hand:

```csharp
Resolve(new[] { "bank", "banks" }, "for each bank in banks", ...)
```

That state is impossible on the real path. `Declarations.Bind` introduces
`bank` inside the loop body, and correctly reports shadowing if `bank` already
exists in the enclosing scope. With the actual enclosing symbol table—`banks`
present, `bank` absent—the resolver returns `NoParse` for the valid loop.

The same value-expression machinery over-accepts things the grammar refuses as
binding names. All of these currently return `Resolved` from the C# resolver:

```ronin
for each (3) in banks
for each (a + b) in banks
for each (a, b) in banks
for each [open order] in banks
for each {open order} in banks
for each (open order] in banks
```

`Scope.Iterating.Variable` accepts one ordinary word or a parenthesized
`Name`; it accepts none of the above. The resolver erases bracket kind into
`LexemeKind.Open`/`Close`, and `Group`/`Unit` never verify matching delimiter
types.

This does **not** reopen the decision about `in`: the pin's extent is
determinate even for the over-accepted cases, so it still cannot swallow the
following word. It does mean the builtin pattern cannot be connected to the
real loop pipeline in its current form, despite the comment saying it will be.

**Recommendation:** represent a binding hole separately from an expression
hole. It should recognize a fresh one-word name or a parenthesized multi-word
name without looking that name up, preserve it as binding metadata/a binding
node, and reject literals, operators, groups of several values, other bracket
kinds, and mismatched delimiters. Alternatively, keep loop binding entirely in
the grammar and resolve only the collection; if so, the builtin pattern must be
documented as reservation metadata rather than a future resolver production.
Test the loop with the symbol table produced by its actual enclosing scope, not
one containing the variable the loop is about to declare.

## 3. Nested ambiguity witnesses can come from an unused parse

**Severity: medium diagnostic correctness in the standalone resolver**

The `REAUDIT5` reproductions now show two witnesses for an ambiguity buried
under a group, operator, or outer call. The implementation finds them by
scanning every DP span, narrowest and leftmost first:

```csharp
for (var width = 1; width < n; ++width)
    for (var i = 0; i + width <= n; ++i)
        if (Expressions(i, i + width, 0).Readings.Count() > 1)
            return ...
```

Nothing requires the selected span to participate in a cheapest parse of the
whole statement.

Confirmed with a table containing the two ordinary ambiguity fixtures:

```text
prefix sum of list + (take from box)
```

The left operand resolves uniquely and cheaply as the whole name
`prefix sum of list`. The top-level ambiguity comes from the bracketed right
operand:

```text
take «from box»
take from «box»
```

The reported witnesses are instead:

```text
sum «of list»
sum of «list»
```

That ambiguous subspan exists inside the left operand, but the selected
top-level reading does not use it. It is simply the first equal-width ambiguous
cell in the global table.

The reproducer uses the same kind of manually assembled, rule-invalid symbol
table as the resolver's existing ambiguity tests; current `Compilation` would
suppress resolution after R5/R6 findings. This limits present source impact,
but it does not make a diagnostic that names the wrong choice correct, and the
standalone resolver explicitly supports these tables.

The code and test comments also say two witnesses are sufficient, while
`Witnesses` returns every distinct reading in the selected cell.

**Recommendation:** carry a bounded pair of witness readings, or witness
provenance, through `Best`, `Offer`, `Merge`, operators, groups, and pattern
matches. A parent with one rendered node and count two must retain the child
pair that made its count two. Do not rediscover provenance by scanning
unrelated cells after resolution.

## 4. `Pattern.Parse` still accepts hand-built segments the lexer can never match

**Severity: medium — silent dead patterns in tests/runtime declarations**

The new parser correctly understands `_` and `(_)`, and it refuses the new
guillemet prose. It does not validate that every other segment is one word
lexeme.

Most notably, the exact notation that escaped from the reference probe is
still accepted:

```csharp
var pattern = Pattern.Parse("take <_>");
// Segments == ["take", "<_>"]
```

`<_>` is stored as a literal word even though `<` and `>` are symbols in the
real lexer. The resulting pattern can never match `take bank` or the lexed
tokens `take < _ >`; it is silently dead. Numbers, operators, punctuation,
embedded tabs, and hyphenated pseudo-words have the same shape. Likewise,
`Pattern.Parse("for each _")` creates two segments where the real lexer emits
the one canonical token `for each`.

The round-trip property does not catch these because an impossible literal can
round-trip to the same impossible literal. Its curated input list tests
rendering identity, not lexical representability.

**Recommendation:** either build this convenience syntax through the real
lexer, with an explicit recognition rule for its hole marker, or validate that
each literal maps to exactly one canonical `Word` lexeme and consumes the whole
segment. Add the literal `<_>` as the regression case, then sweep every
`LexemeKind` and each multi-word keyword. `Pattern.Parse` should return a usable
pattern or reject; a successfully constructed pattern that can never match is
the forbidden third outcome.

## 5. Pinned metadata can describe non-holes and mutate live hash keys

**Severity: medium latent invariant failure**

The direct constructor copies the `pinned` collection but never validates its
indices. Both of these are accepted:

```csharp
new Pattern(["take", null], [0]) // index 0 is the literal "take"
new Pattern(["take", null], [2]) // outside the segment array
```

Both render as `take (_)`. Parsing that rendering returns the ordinary
unpinned pattern, which is unequal to the original. These are constructible
patterns the registry can emit, so they directly falsify the stated universal
round-trip-or-refuse property.

`Pinned` is also exposed as an `IReadOnlySet<int>` whose runtime object is the
mutable `HashSet<int>`:

```csharp
public IReadOnlySet<int> Pinned { get; }
```

An internal caller can downcast and mutate it. `Pattern` is used as a key in
the declaration and runtime dictionaries, and its hash now includes `Pinned`,
so mutating the set after insertion changes the hash of a live key and can
make its declaration unreachable. `SymbolTable.Builtins[0]` exposes the same
process-wide object, so a mutation also changes matching and reservation
behavior globally.

The segment and anchor collection expressions use compiler-generated read-only
wrappers; the pin set is the remaining mutable part of the object.

**Recommendation:** reject every pinned index that is out of range or does not
refer to a `null` segment, and store the result in an immutable/frozen set that
cannot be recovered as a mutable `HashSet`. Test dictionary lookup before and
after attempts to mutate, as well as rendering/refusal for invalid pin
metadata.

## 6. Current comments and registry prose still contradict the settled loop

**Severity: low contract/documentation drift**

The public specification and README are corrected, and the historical handoff
documents are intentionally historical. Several current code/test comments
still state the superseded rule:

- `Compiler/Grammar/Name.cs:41` says single-word `in` is refused for
  legibility and enforced at declaration; it is legal in every position.
- `Test/Unit/Boundaries.cs:108` likewise says `in` is reserved at declaration.
- `Test/Unit/Loops.cs:26` says the loop is safe because a name may not contain
  `in`; names containing it are now explicitly legal.
- `Compiler/Diagnostics/Glue.cs:40` still presents “all words before the first
  hole” as the whole free-pattern rule, omitting determinate holes.

The generated registry has removed `<_>`, which is the important correction.
Its new legend says nothing inside guillemets is source, immediately before
rendering `«for each»`; `for each` is real source and is quoted only to expose
that it is one lexer token. This solves the accidental copy/paste problem by
making the registry describe internal segmentation, but the legend overstates
what the quoting means and does not implement either presentation suggested in
`SWEEPITEMS.md`—a source-facing declaration or separately labelled hole
metadata.

**Recommendation:** update the four live comments. For the registry, separate
source spelling from structural metadata (for example, a source-pattern column
and a `pinned/free/braced` hole-kind column), or narrow the guillemet legend so
it does not call genuine source text non-source.

## The eight `REAUDIT5` repairs

All direct repairs were reproduced:

1. An enclosing `index of bank` followed by `for each bank ...` produces one
   typed `Shadowed` finding with the correct related span; it no longer throws.
2. Adjacent braced list, lookup, and input values without commas produce
   `Malformed`; their comma-separated forms compile cleanly.
3. Invalid `old`, `index`, and `of` pattern shapes produce one structural
   finding independent of the number of mutable variables or loops in scope.
4. Multi-word keyword anchors resolve over single spaces, repeated spaces,
   tabs, and newlines. Finding 1 is the remaining declaration-side half, not a
   failure of anchor canonicalization.
5. The new nested-ambiguity cases show two readings. Finding 3 is the
   provenance case the selected examples do not exercise.
6. `NameInvariant` now tests `Resolver.CanName` directly over every
   `LexemeKind`, including both ends of a span.
7. The README, specification, generated registry, and main loop comments use
   the current spelling and zero-reservation decision. Finding 6 lists the
   remaining live contradictions.
8. `Anchor` is cached, and valid pinned/unpinned patterns now have distinct
   stable hashes. Finding 5 concerns invalid and mutable pin metadata.

## The two sweep items

- **Pattern rendering:** valid free-hole patterns round-trip; pinned and
  multi-word-token renderings are visibly editorial and are refused. `<_>` no
  longer appears in generated output. Findings 4 and 5 are the unquantified
  construction cases outside the curated property set.
- **`return 1 return 2`:** the parser preserves it as one block element, the
  resolver returns `NoParse`, and `return return 1` resolves at two lookups.
  The CLI still exits zero for the malformed return because resolution is not
  joined to `Compilation`; the commit and tests state this openly. That is the
  acknowledged pipeline deferral rather than a separate parser fix to add.

## Requested high-risk areas that passed

- **`old` as glue:** a structurally invalid shape is excluded from R5 before
  scope-size amplification. The catastrophic one-finding-per-variable form is
  gone.
- **Name absence rather than cost:** the structural word-only predicate is now
  directly asserted, while the older cost consequence remains.
- **Finding renderer totality:** every current `FindingKind` has one typed
  subclass and the golden rendering contains the removed/added-kind changes.
- **Source-reachable `Pattern` construction:** leading and over-wide user
  patterns still become findings before the throwing constructor. No new
  source-reachable constructor throw was found. Findings 4 and 5 concern direct
  internal construction paths.
- **Aggregate separator policy:** the closing-brace exemption is confined to
  `Terminal` aggregates and still covers statement/type bodies.

## Validation

- Debug: 630 tests passed.
- Release: 630 tests passed.
- Release coverage: 100% line, branch, and method.
- Release solution build: zero warnings and zero errors.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical free-hole checks passed. It remains
  historical evidence; it does not model the current binding-hole integration
  described in finding 2.
- `git diff --check`: clean.
- CLI and real lexer/declaration/resolver probes were used for findings 1 and
  2; temporary probe files were removed.

The documented hand-aligned `dotnet format` differences are settled project
policy. They are not a finding and are not being raised again.

## Known outstanding work, not rediscovered here

The following remains acknowledged project work rather than a failed repair:

- joining resolution and later semantic/runtime phases to `Compilation`,
  including surfacing `NoParse` for `return 1 return 2`;
- dangling `=>` and return-type work still remaining from the earlier finding;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing the bounded exponential brace parse with one parse/one decision;
- the resolver allocation/pooling wins;
- the unimplemented items in `FAILUREMODES.md`, including module-composition
  semantics, recomputation cutoff, and live-edit lifetime.

Those known items and the findings above prevent release-level sign-off.

## Recommended order

1. Centralize canonical token identity across names, declarations, patterns,
   completion, and resolution.
2. Decide and implement the resolver boundary for the loop's binding hole;
   keep the zero-glue decision.
3. Carry bounded ambiguity-witness provenance through the DP.
4. Make `Pattern.Parse` lexical and validate/freeze pin metadata.
5. Synchronize the remaining live comments and clarify the registry columns.
