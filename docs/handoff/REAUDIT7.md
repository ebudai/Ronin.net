# Seventh re-evaluation — canonical identity, binding semantics, and repair edges

Audited at commit `297fa1e` (`Close REAUDIT6`), against the previous audited
commit `aa00249`.

This review reproduced every `REAUDIT6` finding through the layer it was meant
to repair, then treated the resulting canonical-name, binding-hole,
ambiguity-witness, and pattern-construction code as new audit surface.

Sign-off is withheld. Four of the six direct repairs are complete. Canonical
identity and the binding hole are only partially complete: the main examples
now pass, but their advertised invariants do not. The fresh pass found three
source-visible defects, two standalone/internal invariant failures, one editor
hot-path pessimisation, and a CI-breaking duplicated test case.

## 1. Canonical name identity still becomes an ambiguous space string

**Severity: high — R5 can be bypassed by ordinary source**

`Name.Canonical` is the right representation, and `Identifier.TryPattern` now
uses it. The representation stops at that boundary. Diagnostics still render a
name and split it:

```csharp
var words = name.Split(' ');
```

That cannot recover token boundaries. A multi-word keyword such as `part of`
is one canonical word token whose identity contains a space. Splitting it
produces the two unrelated words `part` and `of`.

This source produces no findings:

```ronin
var hello part of alice => Number;
function send (x => Number) via part of (y => Number) { return x; }
```

The function declares these pattern segments:

```text
send | hole | via | part of | hole
```

Both `via` and the single token `part of` are glue. The variable's canonical
sequence is:

```text
hello | part of | alice
```

R5 should therefore reject it. `Rules.Offender` instead examines
`hello | part | of | alice`, does not find the `part of` segment, and accepts
the declaration. The same split is repeated when the finding chooses which
word to quote.

This is exactly the safety rule that exists to prevent silent capture, not a
display discrepancy. The commit says that nothing renders a name and splits it
again; `Rules.cs:187` and `Rules.cs:237` still do.

There are two more signs that canonical identity is not yet the identity:

- `Name.Equals` and `GetHashCode` still use raw token text. Names spelled with
  `part of` and `part  of` have equal `Words` and equal `Canonical` sequences
  but compare unequal and hash differently.
- A source-declared pattern containing `part /* gap */ of` has segments
  `part | of`, renders as `part of`, and parses back with the one segment
  `part of`. Thus a pattern reachable from source still fails the stated
  round-trip-or-refuse property. Whether a comment is allowed to interrupt a
  composite keyword does not change the defect: the compiler constructed one
  segment sequence and its renderer reconstructs a different one.

The symbol table's `string` keys and resolver's `string.Join` have the same
structural limitation: `["part of"]` and `["part", "of"]` both become
`"part of"`. Current resolution happens to accept either spelling against that
key, but it cannot preserve the sequence the new abstraction says is identity.

**Recommendation:** carry an immutable canonical word sequence on every
declared name and use it for equality, hashing, R5, completion, and pattern
construction. Keep a rendering beside it only as presentation or as a key
whose lossiness is explicitly accepted. No consumer that needs token identity
should reconstruct it from the rendering.

Add the exact R5 source above, canonical `Name` equality across all whitespace
spellings, and a source-pattern round trip with trivia separating the two
ordinary word tokens.

## 2. The binding hole recognizes a declaration but still produces a value read

**Severity: high integration blocker**

The new `Binding` routine correctly fixes the immediate extent and lookup
problems:

- `for each bank in banks` resolves without `bank` in the enclosing table;
- the six expression/bracket cases from `REAUDIT6` now return `NoParse`;
- a valid binding contributes no lookup cost.

It does not preserve that decision in the tree. `Binding` returns
`Node.Name`, whose contract is “a name in scope; one lookup.” `Evaluator`
evaluates every call argument, and every `Node.Name` becomes:

```csharp
graph.Read(name.Words)
```

A focused end-to-end tree/evaluator probe resolved:

```ronin
for each bank in banks
```

against an enclosing table containing only `banks`. The first call argument
was `Node.Name("bank")`; evaluation returned the error that `bank` was not
declared. The resolver now knows this occurrence declares a name, then erases
that fact into the node for a value reference.

This is not the acknowledged fact that resolution has not yet been joined to
`Compilation`. Joining it now would expose the failure. There is no binding
metadata for that join to consume, and the generic runtime `Declaration`
eagerly evaluates all hole arguments as values.

The recognizer also disagrees with the grammar about which word spans are
names. `LexemeKind.Word` includes keywords, whereas `Name.Parse` rejects a name
beginning with a non-modifier keyword. All of these resolve but produce a
`Malformed` finding through the grammar:

```ronin
for each if in banks
for each while in banks
for each part of in banks
for each (if ready) in banks
```

The prior six rejection tests cover values and bracket shapes, but not the
grammar's keyword boundary.

**Recommendation:** introduce a binding node/argument kind, or preserve
binding metadata on `Node.Call`, and give it a non-eager runtime path. Share
the grammar's declaration-name predicate with the binding recognizer rather
than treating every resolver `Word` as bindable. Test parser and resolver
acceptance as the same matrix, then test that evaluating the resolved tree
does not read the declared name from the enclosing graph.

## 3. The “first keyword only” check restarts after every parameter block

**Severity: medium — documented source pattern is rejected**

`Name.Parse` correctly prevents a production keyword from being swallowed at
the beginning of an identifier. `Identifier` invokes it separately for every
name component, so the same guard also fires after a parameter block:

```ronin
function send (x => Number) part of (y => Number) { return x; }
```

The compiler reports one `Malformed` finding, “expected definition,” because
the identifier stops after the first parameter. This is not the rule described
by the current comment or guide. They say the first word/component of the
identifier may not be a keyword; here `part of` is in the middle, where no
outer production can steal the declaration.

It also leaves the new canonical-keyword tests narrower than they appear. They
prove a composite keyword in the anchor before the first hole. They do not
prove the same token in glue position immediately after a hole, because that
source never reaches declaration construction.

**Recommendation:** enforce the production-keyword rule once at
`Identifier.Parse`'s beginning, not every time `Component.Parse` asks for
another `Name`, unless the intended language rule is actually “every name
component.” If the latter is intended, update the specification and the
comments and account for the pattern shapes it removes.

Add keyword-led name components after a parameter block for every keyword
class, plus controls showing that the same keyword remains rejected at the
identifier's beginning.

## 4. Pattern construction still accepts wrong hole brackets and dead direct segments

**Severity: medium — incomplete construction invariant**

`Pattern.Parse` now uses the real lexer and correctly refuses numbers,
operators, punctuation, `<_>`, and malformed partial holes in the current
tests. Its `Hole` helper checks only the broad `Open` and `Close` kinds, not the
actual bracket text or a matching pair.

Consequently all of these silently become the ordinary free hole:

```csharp
Pattern.Parse("take [_]");
Pattern.Parse("take {_}");
Pattern.Parse("take (_]");
Pattern.Parse("take [_}");
```

This is more than permissiveness in a convenience parser. `(_)` is the only
declared free-hole notation, and the design documents reserve braced units for
a different future hole kind. The current parser consumes that future syntax
as today's free hole.

The direct constructor has the complementary gap. It validates the first
segment, width, and pins, but never validates a non-null literal segment.
These are accepted as patterns:

```csharp
new Pattern(["take", "<_>", null]);
new Pattern(["take", "1", null]);
new Pattern(["take", "+", null]);
new Pattern(["take", "for  each", null]);
```

No corresponding source can match any of them. The last lexes canonically as
the single segment `for each`, not the doubled-space string stored by the
constructor. The forbidden third outcome—successfully constructed but
unmatchable—therefore remains available through the constructor that every
runtime and registry caller can use.

**Recommendation:** make the constructor the invariant boundary. Every
non-null segment must lex to exactly one canonical `Word` lexeme whose text is
equal to the stored segment. `Pattern.Parse` should recognize exactly `(`,
`_`, `)` for its free hole, then delegate to that constructor. Add every
bracket-pair combination and the direct dead-segment matrix.

## 5. Duplicate pattern entries produce ambiguity with no witnesses

**Severity: medium standalone-resolver diagnostic invariant**

Witness provenance now follows the selected derivation, and the
`REAUDIT6` wrong-cell reproducer reports the correct `take` pair. The remaining
same-rendering case contradicts the new cell comment.

This table is accepted by the standalone API:

```csharp
var symbols = new SymbolTable()
    .WithNames("x")
    .WithPatterns("take _", "take _");

var resolution = new Resolver(symbols).Resolve("take x");
```

The result is:

```text
Kind     = Ambiguous
Readings = []
```

`Cell.Offer` keys both nodes by the same rendering, correctly leaves one node
in `order`, but adds the second derivation to that rendering's count. `Count`
becomes two while `Witness` for the sole rendering remains empty.

The code comment explicitly says that two derivations with the same rendering
are the same reading and must not report a tie with itself; the implementation
does exactly that. Source declarations currently deduplicate pattern shapes
before the resolver, which limits present source impact, but `WithPatterns`,
`Patterns.Add`, and repeated/overlapping `Merging` all permit this state.

**Recommendation:** define ambiguity as multiple distinct semantic readings,
not multiple offers of one rendering. Duplicate offers of one unambiguous
reading must remain count one. If equal renderings can hide genuinely distinct
trees, stop keying semantic identity on rendering and give witnesses a
non-display key. Assert that every `Ambiguous` result has at least two distinct
witnesses.

## 6. The exact CI build fails on a duplicated theory row

**Severity: medium — current branch cannot pass its build gate**

`Test/Unit/Resolutions.cs` contains the same row twice:

```csharp
[InlineData("take (")]
```

The ordinary build emits `xUnit1025`, and xUnit reports that one case is
skipped because it has the same test ID. The exact workflow command is:

```text
dotnet build --no-restore --configuration Release -warnaserror
```

and fails with:

```text
error xUnit1025: Theory method
'ASegmentTheLexerCannotMakeIsRefusedNotStored' ... has InlineData duplicate(s)
```

This is not one of the settled `dotnet format` whitespace differences. It is a
compiler/analyzer diagnostic promoted to an error by the committed workflow,
and one purported regression row does not execute independently.

**Recommendation:** remove the duplicate row, run the exact workflow build
locally, and retain the other unmatched-open and malformed-hole rows.

## 7. Completion re-lexes every name once per suffix on every request

**Severity: low — new editor hot-path allocation and CPU pessimisation**

Replacing `name.Split(' ')` in completion restored the composite-keyword
example, but did so by calling `Lexemes.Words(name)` inside both loops:

```csharp
for (var start = 0; start <= typed.Length; ++start)
    foreach (var name in symbols.Names)
        var words = Lexemes.Words(name);
```

`Lexemes.Words` constructs a lexer, token chain/list, canonical strings, and a
new array. For a trailing run of `t` words and `n` names, one completion request
re-lexes stored names `(t + 1) * n` times. Completion is an editor keystroke
path, and neither the names nor their canonical sequences changed between
suffixes.

This is also architectural evidence for finding 1: the first-class sequence
was not carried to the consumer, so the consumer has to guess it again from
display text.

**Recommendation:** store the canonical sequence with each symbol-table name,
or build one immutable completion index when the table changes. At minimum,
lex each name once outside the suffix loop. Add an allocation/counting test or
benchmark with many names and a long trailing word run.

## The six `REAUDIT6` repairs

The direct results are:

1. **Canonical token identity: partial.** Whitespace variants now declare the
   same key, source-declared composite-keyword anchors resolve, and
   `Identifier.TryPattern` no longer splits. Finding 1 is the still-rendered
   diagnostics/equality boundary; finding 7 is the re-lexing workaround.
2. **Binding hole: partial.** The enclosing-scope and invalid-value examples
   pass. Finding 2 is the erased binding semantics and parser/resolver keyword
   mismatch.
3. **Ambiguity provenance: direct fix passes.** The selected nested ambiguity
   supplies its own witnesses. Finding 5 is the separate same-rendering
   invariant.
4. **Lexical `Pattern.Parse`: partial.** The listed non-word segments are
   refused and multi-word keywords parse canonically. Finding 4 covers bracket
   identity and the unchecked direct constructor.
5. **Pin metadata: passes.** Invalid pin indices/non-holes throw, the set is
   frozen, and dictionary identity is stable.
6. **Comments and registry prose: passes.** The four live contradictions are
   corrected and the registry distinguishes source text from the pinned-hole
   prose.

## Known-stricter implementation choices

The two points supplied before the review are not findings:

- `LeadingHole` rejects every leading hole although the settled design can
  describe a leading bracket-required hole. No declaration syntax can express
  that hole kind, so the extra strictness remains unobservable.
- R6 compares anchor runs rather than determinate prefixes. Those sets are
  identical while source patterns cannot contain braced or pinned holes.

## Validation

- Locked restore succeeded without changing any lock file.
- Debug: 651 tests passed; xUnit reported one duplicate-ID case skipped.
- Release: 651 tests passed; xUnit reported the same duplicate-ID case skipped.
- Release coverage: 100% line, branch, and method.
- Non-incremental Release build without warning promotion: one warning,
  `xUnit1025`.
- Exact CI Release build with `-warnaserror`: failed with one error,
  `xUnit1025`.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical free-hole checks passed. It remains
  historical evidence and does not model the current binding node/runtime
  problem.
- `git diff --check aa00249..297fa1e`: clean.
- Eighteen focused audit probes reproduced the findings above and were removed.

The documented hand-aligned `dotnet format` differences are settled project
style. They were not treated as a finding and are not being raised again.

## Known outstanding work, not rediscovered here

The following remains acknowledged project work:

- joining resolution and later semantic/runtime phases to `Compilation`,
  including surfacing `NoParse` for adjacent return expressions;
- the remaining dangling `=>` and return-type work;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing the bounded exponential brace parse with one parse/one decision;
- the previously identified resolver allocation/pooling wins;
- the unimplemented items in `FAILUREMODES.md`, including module-composition
  semantics, recomputation cutoff, and live-edit lifetime.

Finding 2 must be resolved before the loop resolver can safely participate in
that pipeline join. These acknowledged items plus the seven findings above
prevent release-level sign-off.

## Recommended order

1. Preserve canonical word sequences through diagnostics, equality, symbol
   storage, and completion; close the source-reachable R5 bypass.
2. Represent binding occurrences in the resolved tree and align their name
   predicate with the grammar.
3. Fix the identifier-level keyword boundary and add after-hole source tests.
4. Put complete lexical and bracket invariants in the `Pattern` constructor.
5. Correct same-rendering ambiguity counting.
6. Remove the duplicated theory row so CI runs again.
7. Cache/index completion word sequences.
