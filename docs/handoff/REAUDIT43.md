# Re-audit 43 — refinement ordering, structural filtering, and lookup identity

**Re-audited:** `e34e9b2..4194b7b`

**Date:** 2026-08-05

## Result

**Two medium-severity diagnostic/relation findings and two low-severity
safeguard/performance findings. No sign-off.**

Seven of FRESHAUDIT6's eight findings are fully resolved. The shared R7b
derivation removes the cubic name multiplier and its allocating LINQ slices;
the first-hole case is now left to R6; the cost prose is honest; the registry
prints synthetic refinement pairs; key comparison retains token boundaries;
ordinary lists no longer allocate lookup key snapshots; the all-glue message is
honest about its blanket rule; and the XML comments are repaired behind a new
semantic gate.

The remaining correctness issues are boundaries of the new R7 representation.
An R7 conflict has three declarations, but the diagnostic orders only the name
and the longer pattern, so it blames the wrong declaration in two of the six
possible source orders. Separately, the existing `sound` pattern filter is still
applied only to R5 glue: patterns already rejected for `old`, an operator word,
or an injection word continue to generate R6b/R7 findings against otherwise
innocent names.

Two smaller gaps remain. The refinement relation compares segment spellings but
not the holes' pinned/free semantics, so a later pinned hole reserves a prefix
that cannot create the rival reading. And the new length-prefixed lookup key
encoder calls the allocating `Token.Canonical` getter twice per token.

## Disposition of FRESHAUDIT6

| Prior finding | Re-evaluation |
|---|---|
| 1. R7b cubic validation | **Resolved.** The relation is derived once per pattern set, indexed by word, and compared by indices. The new scaling test passes. |
| 2. Duplicate-key boundary collision | **Resolved.** Key identity is length-prefixed by canonical token; `a bc` and `ab c` remain distinct through source. |
| 3. First-hole R7 after R6 | **Resolved.** The first hole is skipped, and the maintained test now includes the triggering name and asserts the complete finding set. |
| 4. False cost and wrong declaration | **Partially resolved.** Equal-versus-cheaper prose is fixed, and name-versus-longer-pattern order is fixed. Finding 1 below covers the unhandled shorter-pattern order. |
| 5. Registry omits R7 | **Resolved.** The registry consumes the shared relation and a synthetic pair pins the output. |
| 6. Lookup work taxes lists | **Resolved for the reported cost.** Key capture happens only after `=`, and `Key.ToArray()` is gone. Finding 4 is a smaller encoder-local remainder. |
| 7. One-word all-glue message | **Resolved.** It now describes a conservatively refused class rather than claiming the present declaration already made a tie. |
| 8. Stacked XML summaries | **Resolved.** The sweep repaired all current duplicates and emitted documentation is gated against recurrence. |

## Findings

### 1. R7 orders only two of its three declarations

**Severity: medium — the diagnostic can point backward and ask an earlier,
innocent declaration to change**

An R7 refinement exists only when three declarations coexist:

1. the shorter pattern;
2. the longer/refining pattern; and
3. the absorbing name.

`Rules.Refining` compares only the name with the longer pattern
(`Compiler/Diagnostics/Rules.cs:315-326`). The shorter pattern's span is neither
considered for the primary nor attached as a related location. That is correct
only when the shorter pattern was already present before one of the other two.

This source orders the longer pattern first, the name second, and the shorter
pattern last:

```ronin
function send (x => Number) to all (y => Number) { return x; }
var all things => Number;
function send (x => Number) to (y => Number) { return x; }
```

The last declaration is what completes the conflict. Expected: a caret on line
3 and a message saying the shorter pattern cannot be declared while the other
two exist. Actual:

```text
P.ron:2:5: «all things» cannot be declared ...
```

The name is blamed because it is later than the longer pattern, even though it
is earlier than the shorter one. The mirror order—name, longer, shorter—blames
the longer pattern rather than the final shorter one. Thus two of the six
permutations are wrong. A local shorter pattern arriving beneath inherited
longer/name declarations has the same failure.

The representation already carries both pattern shapes and spans in
`Refinement` (`Rules.cs:60-65`); the diagnostic drops one at the final join.

**Recommendation:** select the latest declaration across the name, shorter
shape, and longer shape, respecting inherited scope provenance. Make that one
the primary and attach both other declarations as related spans with their
roles. The finding/message needs three repair cases rather than the current
`Blamed` boolean. Test all six same-scope permutations and the shorter-pattern-
inside case.

### 2. Structurally invalid patterns still reserve names through R6b and R7

**Severity: medium — one invalid pattern produces downstream findings whose
only repair is already stated by the structural finding**

`Rules.Validate` sends the full pattern set through `Anchors`, `Shadowing`, and
`Refining` before it computes `sound` at lines 100-109. The filter is used only
by `Glue`, even though its own comment states the broader invariant: a pattern
wrong in itself does not then get to reserve words. `Structural` already knows
all three invalid classes (`Rules.cs:164-173`).

Two real-source probes reproduce both relational leaks.

R7 through an operator word:

```ronin
var otherwise things => Number;
function send (x => Number) to (y => Number) { return x; }
function send (x => Number) to otherwise (y => Number) { return x; }
```

Actual findings:

```text
InfixInPattern
NameAbsorbsRefinement
```

The second pattern is already forbidden for using `otherwise`. It therefore
cannot coexist with the shorter pattern and cannot reserve `otherwise …`
against names. The R7 finding has the same repair as the first.

R6b through `old` in an anchor:

```ronin
var compute old things => Number;
function compute old (x => Number) { return x; }
```

Actual findings:

```text
NameShadowsPattern
ReservedSegment
```

Again, the invalid pattern reserves a prefix before its structural finding is
allowed to remove it. More matching names scale this into one extra finding per
name, which is the amplification the existing `sound` comment explicitly says
it prevents.

**Recommendation:** compute `sound` before the relational rules. Run structural
diagnostics (`Infixes(patterns)`, `Reserved`, and `Injecting`) over all patterns,
but run `Anchors`, `Shadowing`, `Refining`, and `Glue` over the sound set. Add one
source regression for an R6b prefix and one for an R7 refinement; the latter
should include several matching names to pin non-amplification.

### 3. R7 treats a pinned hole as free and reserves a prefix with no rival reading

**Severity: low — latent safeguard error in a constructible pattern shape**

The settled R7 definition inserts words at the start of a **free** hole. A
pinned hole takes exactly one word or one bracketed name
(`Compiler/Resolution/Resolver.cs:432-454`), so it cannot consume the multi-word
name that would be needed for the shorter rival.

`Refines` compares only `Segments` (`Compiler/Diagnostics/Rules.cs:380-399`). It
never reads `Pattern.Pinned`, even though pinning is explicitly part of pattern
identity (`Resolver.cs:902-903, 1073-1074`).

The focused constructor witness is:

```csharp
shorter = new Pattern(["send", null, "to", null], [3]);
longer  = new Pattern(["send", null, "to", "all", null], [4]);
```

Both trailing holes are pinned. `Rules.Refinements([shorter, longer])` currently
returns `all`. But `send x to all things` cannot read through the shorter
pattern: its pinned trailing hole consumes `all`, leaving `things` behind. The
longer pattern consumes the literal `all` and pins `things`, so there is one
reading rather than an R7 rivalry.

This is not source-observable today: source has no pin declaration syntax, and
the one built-in pin is the first hole, which R7 skips. It is nevertheless
reachable through the pattern constructor used by built-ins and makes both the
shared relation and registry wrong as soon as a later hole is pinned.

**Recommendation:** require the refined hole in the shorter pattern to be free,
and compare pin membership for every corresponding hole before and after the
inserted run. Pin the relationship with both `Rules.Refinements` and a resolver
witness proving that the purported shorter reading does not exist.

### 4. Lookup identity computes each allocating canonical spelling twice

**Severity: low — a newly allocation-conscious path performs duplicate work per
key token**

The length-prefixed identity is correct, but its loop reads `token.Canonical`
once for `Length` and again for `Append`
(`Compiler/Grammar/Collection.cs:202-213`). For ordinary tokens the base getter
is `Memory.ToString()` (`Compiler/Lexicon/Token.cs:31`), so each read allocates a
new string. Composite keywords can do still more normalisation work.

A warmed Debug probe compiled a lookup with 500 one-token keys:

```text
current:                         2,933,416 bytes
cache Canonical once per token:  2,914,216 bytes
difference:                         19,200 bytes
```

That is 38.4 bytes per key and 0.65% of the entire compile for a one-line local
change. Multi-token keys scale the duplicate work by token count. The temporary
production change was reverted after measurement.

**Recommendation:** assign `var canonical = token.Canonical` once inside the
loop and use it for both length and content. If lookup work later replaces the
encoded string with a structural sequence comparer, that subsumes this; until
then the local cache preserves the present design without duplicate allocation.

## Verification performed

- Inspected all three incorporating commits and their complete compiler/test
  diff, then re-ran every original witness against the maintained tests.
- Focused changed-surface suite: **204 passed**.
- New source probes reproduced the three-declaration ordering error and both
  structurally-invalid-pattern amplifications.
- A constructor probe reproduced the pinned-hole false refinement.
- The lookup allocation probe measured the duplicate canonicalisation; all
  temporary code and the temporary production edit were removed/reverted.
- 20,000 deterministic malformed ASCII sources passed through
  `Compilation.Of` without an unhandled exception.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Exact Release coverage gate: **1,073 passed, 0 failed, 0 skipped; 100% line,
  branch, and method coverage**.

`git diff -- Compiler Test` was empty after probe cleanup. The pre-existing
`docs/spec` edits and untracked handoff/design files were preserved.

## Settled and deferred work

- The remaining lookup runtime/equality work and earlier finding 9 remain
  explicitly deferred until the lookup representation lands; they are not
  counted again here.
- The leading-hole decision, multi-word operator work, differential change
  reports, and other disclosed future slices were not recast as defects.
- Owner-authorized warning suppressions remain for their dedicated round.
- The hand-aligned `dotnet format` whitespace differences remain settled style
  and are explicitly not a finding.
