# Fifth re-evaluation — hybrid audit

Audited at commit `0f87712` (`Note that every quoted figure is now reproducible`).

This review treated `bfd2c1c..0f87712` as a re-audit of the four earlier
reports and as a fresh audit of the loop, pinning, diagnostics, aggregate, and
reflection work added since them. It also followed the designer's four
explicit requests:

- prove whether `old` is actually prevented from becoming glue;
- check that `NameInvariant` asserts absence rather than merely cost;
- check renderer totality and the golden output after the finding changes;
- sweep source-reachable construction for throws rather than findings.

Sign-off is withheld. Two ordinary source files can currently produce a fatal
exception or silently lose a required comma, and invalid glue patterns still
produce the diagnostic cascade the new rules say they prevent.

## 1. An existing derived loop-counter name terminates compilation

**Severity: high — source-reachable unhandled exception**

`Declarations.Bind` runs `Refused` for the loop variable, but not for its
derived `index of <variable>` name:

```csharp
var counter = Index + name;

written[counter] = span;
Symbols.WithNames(counter);
symbols.Add(new Declared(counter, span, InjectedBy: name));
```

If that counter already exists in an enclosing scope, the symbol-set insertion
quietly does nothing while the diagnostic metadata receives a duplicate
`Declared`. `Rules.Glue` then assumes names are unique and calls
`ToDictionary`, which throws.

Reproduced through the command-line compiler:

```ronin
var index of bank => Number;
for each bank in banks { return bank; }
```

Observed result:

```text
Unhandled exception. System.ArgumentException:
An item with the same key has already been added. Key: index of bank
    at Ronin.Compiler.Rules.Glue(...)
```

This is the missing opposite-order case in `LoopIndex`: declaring the same
name *inside* the loop correctly produces `Shadowed`, while declaring it
before the loop kills the process.

**Recommendation:** validate the derived counter through the ordinary refusal
path, or an equivalent dedicated path, before mutating `written`, `Symbols`, or
`symbols`. The finding should point at and name the loop variable—the origin
the author can rename—while relating the existing counter declaration. Add
both declaration orders as end-to-end tests. Keeping `Rules.Glue` tolerant of
duplicate metadata would be useful hardening, but it is not a substitute for
refusing the collision at its source.

## 2. The block-terminator exception also removes commas from nested values

**Severity: high language correctness**

The intended rule is sound: a statement ending in a braced block does not need
a following semicolon. The implementation lives in generic
`Aggregate<TParent, TOpen, TElement, TSeparator, TClose>`, however:

```csharp
if (parser.TryAdvance<TSeparator>() is false)
{
    if (Ended(started, parser) is Close.Brace) continue;
    ...
}
```

That same class parses comma-delimited lists and lookups. Consequently, *any*
aggregate element ending in `}` receives the statement-only exemption,
regardless of whether its separator is `;` or `,`.

Confirmed through the command-line compiler, with zero findings:

```ronin
var nested values = { { 1 } { 2 } };
```

The two inner lists are accepted without the required comma. This is exactly
the kind of cross-use regression a generic parser creates: the 84 generated
statement programs cover the intended instantiation and cannot observe the
other instantiations whose contract changed.

**Recommendation:** make separator elision an explicit aggregate policy, or
restrict it to the statement/`Terminal` instantiation. Add negative
source-level cases for adjacent braced values in lists, lookups, and input
blocks, alongside the positive statement-shape generator.

## 3. Invalid glue patterns remain active and amplify into one finding per injection

**Severity: medium diagnostics failure; unbounded in scope size**

`Rules.Validate` emits `ReservedSegment` and `InjectionWordAsGlue`, then sends
the same invalid patterns through the ordinary R5 name scan:

```csharp
foreach (var finding in Reserved(patterns)) yield return finding;
foreach (var finding in Injecting(patterns)) yield return finding;
foreach (var finding in Glue(names, patterns)) yield return finding;
```

Thus `old` is recognized as forbidden, but it is not prevented from acting as
glue for the rest of validation. Reproduction:

```ronin
var alpha => Number;
var beta => Number;
var gamma => Number;
function recall (x => Number) old (y => Number) { return x; }
```

The compiler reports one correct `ReservedSegment`, followed by three
`GlueInInjectedName` findings—one for `old alpha`, one for `old beta`, and one
for `old gamma`. Every additional mutable variable adds another complaint at
the pattern the author already knows must be respelled.

The protected injection words have the same defect. The golden file currently
preserves both:

1. `InjectionWordAsGlue` for `item (_) of (_)`;
2. `GlueInInjectedName` for the loop's `index of bank`.

That contradicts the finding's own contract: catch the mistake once at the
offending pattern instead of once per generated name.

**Recommendation:** structurally invalid patterns must not participate in
downstream collision rules. Classify each shape once, emit its structural
finding, and exclude it from `Glue` (and any other consequences whose only
repair is the same respelling). Test exact finding count with zero, one, and
many mutable declarations/loops so scope size cannot change the result.

## 4. Multi-word keyword whitespace is recognized but not canonicalized for resolution

**Severity: medium integration defect**

`Keyword.Spaced` correctly recognizes every nonempty whitespace run, but the
token retains the original source slice. `Lexemes.ToLexemes` then copies that
slice verbatim:

```csharp
lexemes.Add(new Lexeme(KindOf(token), token.Memory.ToString()));
```

The built-in loop pattern's anchor is the canonical string `"for each"`.
Therefore these two paths disagree:

```text
Lexer/grammar:  for  each bank in banks    accepted as ForEach
Resolver:       for  each bank in banks    NoParse
```

Confirmed with the real lexer through `Resolver.Resolve(string)`. The
single-space spelling resolves normally.

This does not yet break `Compilation` because the loop remains a grammar
production and resolution is not joined to it. It will break that planned join,
and it means the accepted B1 condition—whitespace-normalized multi-word
keywords—is presently true only of token *type*, not token identity.

**Recommendation:** give keywords a canonical spelling used by the lexeme
adapter, or match anchors by canonical token identity rather than source text.
Run the existing whitespace matrix through the resolver/built-in pattern too;
testing only `Assert.IsType<ForEach>` cannot catch this split.

## 5. A nested ambiguity is reported with only one of its readings

**Severity: medium diagnostic correctness**

`Cell` tracks all cheapest nodes at its own span, but `TryBest` hands parents
only `order[0]` plus a saturated derivation count:

```csharp
best = new Best(Cost, order[0], Count);
```

The ambiguity bit therefore propagates through a bracket, group, operator, or
outer call while the competing witness trees do not.

With names `list`, `of list`, and `x`, and patterns `sum (_)` and
`sum of (_)`, this statement:

```ronin
(sum of list) + x
```

returns `ResolutionKind.Ambiguous`, but `Resolution.Readings` contains one
entry. The ambiguity message consequently says “bracket an argument to
choose” and prints only one choice, even though the bracket is already where
the hidden ambiguity lives.

The simple top-level ambiguity test passes because both readings are offered
directly to the top cell. `AGroupOfTiesIsStillATieHoweverManyOfThemThereAre`
checks only the kind and likewise does not inspect the witnesses.

**Recommendation:** carry a bounded pair of witness trees through composition,
or report the innermost ambiguous span and its witnesses. Only two are needed
to prove and explain a tie; retaining every reading is unnecessary. Add
operator, group, and outer-pattern cases that require two distinct rendered
witnesses.

## 6. `NameInvariant` still infers absence from the current cost model

**Severity: low now, high-value regression guard missing**

The test explicitly says the swallowing reading is “ABSENT ... proved by
cost,” then compares cost 2 with cost 4. That is a useful end-to-end consequence
test, and the most direct implementation regression today would make it fail.
It is not the negative parse-set assertion the designer requested.

The distinction matters because `Resolution.Readings` exposes only cheapest
readings, and finding 5 shows that even cheapest child witnesses are not
preserved through composition. The test cannot directly ask whether a
bracket-spanning name candidate was constructed; it infers the answer from the
present lookup-cost relationship. A later change to name normalization and
costing can invalidate that inference without changing the two numbers in the
way this test expects.

**Recommendation:** extract the load-bearing predicate into an internal
`IsNameSpan`/`CanName` operation and assert directly that word-only spans pass
while spans containing every bracket and symbol kind fail. Retain the current
capture/cost case as the integration test. Together they assert both the
structural rule and its semantic consequence.

## 7. Public documentation still specifies the superseded loop

**Severity: medium contract drift**

The implementation and generated registry now say:

- the declaring hole is pinned;
- one word is unbracketed, a multi-word variable is bracketed;
- `in` is not reserved;
- a braced statement needs no terminator before the next statement.

The public material says otherwise:

- `docs/spec/grammatical-structure.md` calls the variable unrestricted
  “words,” says `in` is glue and reserved, and says statement sequences must
  end in `;`;
- `README.md` still lists `iterate` as a keyword and demonstrates
  `iterate shoes => shoe`;
- `SymbolTable.Builtins` still comments that `in` is reserved and that the
  built-in exists for its glue;
- the generated registry heading says every zero-cost pattern has all words
  before its first hole, immediately above `for each <_> in (_)`, which visibly
  has `in` after the first hole;
- several boundary and loop test comments still say `in` is reserved.

The historical handoff documents are intentionally historical and are not part
of this finding. The current specification, README, generated output, and live
code comments are.

**Recommendation:** update the public contract in the same change as the code.
Generate or centralize the loop spelling/reservation facts where practical;
the current copies have already drifted in mutually contradictory directions.
Change the registry category explanation from “all words precede the first
hole” to the actual zero-glue condition, which includes protection by pinning.

## 8. R6 allocates anchor arrays quadratically

**Severity: low performance pessimization**

`Pattern` is immutable, but every `Anchor` access constructs a fresh array:

```csharp
public IReadOnlyList<string> Anchor
    => [.. Segments.TakeWhile(s => s is not null)];
```

`Rules.Anchors` uses that property repeatedly inside an ordered
pattern-by-pattern nested loop. Even a pair rejected by the count comparison
allocates two arrays; a possible prefix allocates several more. `Glue` also
allocates one merely to obtain the first-hole index.

The existing resolver allocation work is real and well guarded—149 lexemes
remain below the 20 MB ceiling—but this scope-entry path introduces avoidable
quadratic transient allocation as the pattern table grows.

**Recommendation:** compute the anchor or first-hole index once in the
`Pattern` constructor and retain the immutable result. Include `Pinned` in
`GetHashCode` as well: equality includes it, and omitting it forces pinned and
unpinned variants of the same segments into the same dictionary bucket.

## Requested high-risk areas that passed

- **Pinned matching:** canonical-spelling cases enforce the same promise
  `Pattern.Glue` advertises. A pin consumes exactly one word or one balanced
  bracketed unit; trailing pins also require that unit to reach the end. The
  documented `for each open order in banks => NoParse` cost is present and is
  not a finding.
- **Direct source-to-Pattern construction:** leading holes and over-wide
  patterns are intercepted before the throwing constructor and become typed
  findings. Malformed parameter subtrees are found by the error walk before
  declaration building. No additional direct source path into the `Pattern`
  constructor was found.
- **Renderer totality:** every current `FindingKind` has exactly one typed
  subclass, every kind is produced by a real rule path, every example renders,
  and the golden file contains the removed/added-kind changes. Finding 3 is a
  rule-production defect preserved by that golden file, not a renderer
  totality failure.
- **Reflective error walk:** the grammar wrappers expose their held syntax
  through properties or `IEnumerable`, by-ref-like properties are excluded,
  cycles are reference-deduplicated, and the member cache is concurrent. The
  nested malformed cases and cold parallel compilation tests are credible.
- **Keyword boundary:** a keyword now ends wherever an ordinary word ends; the
  punctuation matrix is comprehensive. Finding 4 concerns canonical spelling
  after recognition, not the boundary fix.
- **Statement block separation:** the intended statement sequences now parse,
  including all 84 generated arrangements. Finding 2 is leakage of that rule
  into other aggregate kinds.

The two documented implementation deferrals are not findings: leading holes
remain stricter than the settled `{_}` design because that syntax is not
declarable, and anchor runs are equivalent to determinate prefixes while user
patterns cannot contain `{_}` or `<_>`.

## Validation

- Release: 604 tests passed.
- Debug: 604 tests passed.
- Release coverage: 100% line, branch, and method.
- Release solution build: zero warnings and zero errors.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical loop-hazard checks passed.
- Findings 1–4 and 5 were exercised through the command-line compiler or the
  real lexer/resolver path, not hand-built tokens.
- `git diff --check` reported no whitespace errors.

The documented hand-aligned `dotnet format` differences are settled project
policy and are not a finding.

## Known outstanding work, not rediscovered here

The following remains acknowledged project work rather than a failed fix in
this review:

- joining resolution and later semantic/runtime phases to `Compilation`;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing the bounded exponential brace parse with one parse/one decision;
- pooling resolver tables for repeated editor calls;
- the unimplemented items in `FAILUREMODES.md`, including module-composition
  semantics, recomputation cutoff, and live-edit lifetime.

Those limitations still prevent a release-level sign-off even after the eight
findings above are corrected; they should remain explicitly tracked rather
than disappearing from successive re-audit reports.

## Recommended order

1. Fix the fatal derived-counter collision and add both declaration orders.
2. Constrain brace-based separator elision to statement aggregates.
3. Stop structurally invalid patterns participating in downstream glue rules;
   update the golden output.
4. Canonicalize multi-word keyword lexemes before the resolver join.
5. Preserve two ambiguity witnesses through nested composition.
6. Add the direct name-span invariant assertion.
7. Synchronize the public specification, README, registry wording, and live
   comments.
8. Cache immutable pattern decomposition and complete its hash.
