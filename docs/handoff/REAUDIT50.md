# Re-audit 50 — journalled equality rollback

**Re-audited:** `adef7d7..30a3c9c`, the commit incorporating `REAUDIT49`.

**Date:** 2026-08-12

## Result

**Sign-off for the REAUDIT49 incorporation. No new findings.**

The cubic-allocation defect is closed without reopening the false-equality defect
that led to it. Candidate comparison now marks an append-only proof journal and,
on failure, removes only pairs added after that mark. Successful candidates keep
their proofs. Nested candidate trials are correctly scoped because each mark is
a position in the same log: an inner failure removes its suffix, and an enclosing
failure can still remove everything added since its earlier mark.

The original 150/300-entry Release probe now allocates approximately 23 KB and
49 KB, respectively, rather than 48 MB and 368 MB. Allocation grows by 2.07×
when the entry count doubles, not the prior 7.68×. The maintained 80/160 ratio
test crosses separately admitted, equal maps with reverse-ordered structural
keys and measures only equality; it therefore guards the actual graph-cutoff
case rather than construction or a scalar-key shortcut.

The prior rollback correctness safeguard remains intact. The memo-poisoning
witness compares unequal in both directions, and its two maps are admitted as
distinct outer keys. The primed graph test continues to assert the ruled lookup
trade: a direct walk sees the recomputed insertion order while the cached
dependent remains unchanged because map equality ignores order.

Section E's type layer remains the explicitly ruled open scope. The expiry
ledger records the temporary “`[]` is always a list” rule and its expected-type
successor, but expected-type construction of `Lookup.Empty` is not implemented.
This report signs off the REAUDIT49 repair, not a claim that the separately open
type-layer portion of Section E is complete.

## Disposition of REAUDIT49

| Prior finding | Re-evaluation |
|---|---|
| Copying the accumulated proof set makes equality allocate cubically | **Closed.** Whole-set cloning is removed. Failed candidates undo only their additions; successful candidates retain them. Original-size allocation falls by four orders of magnitude and doubles rather than growing eightfold when input doubles. |

## Implementation review

### Journal semantics

`Builtin.Same` now carries a `Proofs` context rather than a bare `HashSet`
(`Compiler/Runtime/Values.cs:344-376`). `Proofs.Repeat` performs the same
membership test as before and records only genuinely new pairs in its append-only
log (`Values.cs:441-485`). A candidate captures `Mark` before comparing its key;
if the candidate fails, `Undo` removes the log suffix from both the set and the
log (`Values.cs:397-428`).

The important cases hold:

- A failed candidate cannot leave an unproved pair for a later candidate.
- A successful candidate commits its proofs for shared-DAG reuse.
- A nested failed trial removes only work performed after its own mark.
- If an enclosing key comparison later fails, its older mark removes successful
  inner additions as part of the enclosing rollback.
- A value mismatch ends the whole comparison, so no candidate search continues
  with its state and no local rollback is needed there.
- A repeated pair is not appended twice, so undo never removes a proof that
  existed before the current mark.

Values admitted by this runtime are acyclic, so encountering a recorded pair
again means shared structure rather than recursive in-progress equality. The
journal therefore preserves the prior memo's successful-DAG behavior while
adding the rollback unordered candidate search needs.

### Resource shape

The old implementation copied every proof established by earlier matches for
every structural-key candidate. Reverse insertion order creates O(n²) candidate
comparisons, and copying an O(n)-sized prefix inside them produced O(n³)
allocation.

The journal allocates storage for distinct proofs and removes failed additions
in place. Rollback work is proportional to the current candidate's descent, not
to the history before it. The unordered search remains O(n²) in the worst case,
which the designer ruling expressly accepts until a language-consistent hash is
warranted; the accidental cubic allocation multiplier is gone.

Flat scalar list and lookup comparison retains the lazy context: `Proofs` is
created only when an aggregate descent can produce a reusable pair. The repair
does not impose journal allocation on the common scalar cutoff path.

## Adversarial verification

Temporary probes were added only for execution and removed afterward.

### Original allocation witness

Two separately admitted maps used one-element list keys `[0]` through `[n-1]`,
all mapped to `0`; one map was ascending and the other descending. Construction
and admission occurred before measurement. A collection preceded
`GC.GetAllocatedBytesForCurrentThread`, and the measured operation was only
`Builtin.Same`.

| entries | equality result | prior allocation | current allocation |
|---:|---|---:|---:|
| 150 | true | 47,908,024 bytes | 23,464 bytes |
| 300 | true | 367,895,224 bytes | 48,600 bytes |

Current growth ratio: **2.07×** for twice the entries. The prior ratio was
**7.68×**.

### Independent equality oracle

A seeded 500-case probe built small maps with distinct two-element list keys,
random insertion orders, and randomly retained or altered values. A simple
independent map comparison over the raw arrays supplied the expected result.
`Builtin.Same` agreed for all cases in both argument directions. This exercises
successful and failed candidate rollback beyond the maintained four-entry
witness without reusing the production memo algorithm as its oracle.

## Maintained safeguards

- The allocation regression compares 80 and 160 reverse-ordered structural-key
  maps after a warm-up and rejects the cubic growth ratio without relying on a
  stopwatch.
- The original failed-candidate witness asserts false in both equality
  directions and verifies distinct outer-key admission.
- The shared-DAG test still verifies memo reuse across list and lookup kinds.
- The graph test primes its dependent before the reorder and guards the
  designer-approved cutoff behavior.
- Lookup insertion order, unordered equality, duplicate refusal, structural
  indexing, miss-as-`nothing`, Fault propagation, unknown-kind refusal, Error
  key refusal, depth accounting, and group invariants continue to pass their
  focused tests.

## Verification record

- Inspected the complete `adef7d7..30a3c9c` change and adjacent equality,
  admission, indexing, graph cutoff, and maintained tests.
- Focused Release aggregate/lookup/indexing/expiry suite: **69 passed**.
- Full Debug suite: **1,224 passed, 0 failed, 0 skipped**.
- Full Release coverage suite: **1,224 passed, 0 failed, 0 skipped**.
- Locked restore: passed.
- Warning-as-error Release build: passed with zero warnings and zero errors.
- Release coverage: **3,748/3,748 lines** and **2,579/2,579 branches**
  (100% each; method coverage also 100%).
- Direct plus transitive package vulnerability audit: no known vulnerable
  packages in the configured source.
- `git diff --check` and the incorporating commit's whitespace check passed.

The working tree was clean after temporary probes were removed. This report is
the only artifact added by the re-audit.
