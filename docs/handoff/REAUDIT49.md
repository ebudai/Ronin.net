# Re-audit 49 — correct rollback, cubic copying

**Re-audited:** `b98c550..adef7d7`, the commit incorporating all three
`REAUDIT48` findings.

**Date:** 2026-08-12

## Result

**No sign-off. The three REAUDIT48 findings are functionally closed, but their
equality repair introduces one high-severity resource defect: comparing equal
lookups with reverse-ordered structural keys allocates cubically in the entry
count.**

The correctness repair itself works. A failed aggregate-key candidate now runs
against a private trial memo and commits it only when that candidate matches.
The exact false-equality witness compares false in both directions and the two
maps remain admissible as distinct outer keys. The maintained regression crosses
the production admission and equality paths.

The cutoff safeguard is now honest. It primes `first key` at `"a"` before the
write, directly observes the recomputed table walking from `"b"`, and confirms
the dependent remains at `"a"` under the designer-approved order-insensitive
cutoff. The ruling, amended audit dispositions, and probe scripts are committed;
the stale canonical-sort comments are corrected; and `EAGGREGATES2.md` §8 now
has a direct supersession banner.

The remaining problem is how rollback was implemented. Every aggregate-key
candidate copies the entire accumulated `HashSet` before comparison. In an equal
map written in reverse order, there are quadratically many candidates and the
set being copied grows linearly with the number of keys already matched. The
result is cubic allocation. A 300-entry equality allocated approximately 368 MB
after both maps were already admitted; doubling from 150 to 300 entries increased
allocation from approximately 48 MB to 368 MB, almost exactly the eightfold
growth of a cubic path.

Section E's type layer remains the explicitly ruled open scope. Its expiry entry
is present, but expected-type construction of `Lookup.Empty` is still not
implemented and must remain outside a Section E completion claim.

## Disposition of REAUDIT48

| Prior finding | Re-evaluation |
|---|---|
| 1. Failed lookup candidate poisons the equality memo | **Correctness closed.** Trial state is discarded on failure and committed on success. Both equality directions and outer-key admission are maintained. The whole-memo copy used to obtain that isolation creates finding 1 below. |
| 2. Cutoff test never establishes cached state | **Closed.** The test reads `first key` before the write and asserts the ruled `table = "b"`, dependent `= "a"` result afterward. |
| 3. Ruling untracked and comments prescribe sorting | **Closed.** The ruling and amended documents are tracked, the direct comments describe unordered map equality, and superseded §8 carries a banner. |

## Finding

### 1. Copying the accumulated proof set for every candidate makes equality allocate cubically

**Severity: high — a few hundred ordinary structural keys allocate hundreds of
megabytes in one equality, and the same equality runs in graph cutoff on
recomputed values.**

The repaired candidate loop is at `Compiler/Runtime/Values.cs:397-428`. Before
testing any aggregate key against any candidate, it executes:

```csharp
var trial = Aggregate(key) ? [.. proven] : proven;
```

The copy is semantically isolated, and a successful trial is correctly committed
at line 420. Its cost depends on all three dimensions of the search:

1. unordered equality may try O(n²) candidate pairs when two equal maps have
   opposite insertion orders;
2. every successful structural-key match adds another pair to `proven`; and
3. each later candidate copies all pairs accumulated from earlier matches.

The total copied entries are therefore proportional to
`sum(i * (n - i))`, which is O(n³). The design makes the witness a central case,
not a hostile representation: the two maps are equal precisely because insertion
order is ignored, and one-element lists are ordinary admitted structural keys.

The temporary Release probe constructed two separately admitted maps with keys
`[0]` through `[n-1]`, all values `0`, one map ascending and one descending.
Admission completed before measurement. After a collection, it measured only
`Builtin.Same` with `GC.GetAllocatedBytesForCurrentThread`:

| entries | result | equality time | allocated by equality |
|---:|---|---:|---:|
| 150 | equal | 55.8 ms | 47,908,024 bytes |
| 300 | equal | 262.7 ms | 367,895,224 bytes |

Doubling the input multiplied allocation by 7.68. Timing also worsened sharply,
but allocation is the stable evidence and follows the project's existing cost
test practice. The probe excluded construction and admission, so the measured
bytes belong to equality rather than the ruling's accepted O(n²) duplicate scan.

This is particularly costly in the reactive runtime. `Graph.Recompute` invokes
`Builtin.Same` for cutoff (`Compiler/Runtime/Graph.cs:1527-1533`), and unchanged
recomputes are the case where the whole equality must succeed. A 300-entry map
can therefore create roughly 368 MB of transient allocation on one settle even
though its value did not change.

The maintained safeguards do not constrain this path:

- `ASharedSubtreeIsComparedOnceNotOncePerPathThatReachesIt` uses scalar lookup
  keys (`"left"` and `"right"`), so `Aggregate(key)` is false and the new copy
  never executes.
- `AKeyCandidateThatFailedLeavesNothingBehindToMakeALaterOnePass` correctly
  guards rollback semantics, but has four entries and does not measure work.
- 100% line and branch coverage executes the copy without constraining how much
  it copies or how often.

**Recommendation:** preserve transactional semantics without cloning prior
proofs. Give the proof context a checkpoint/undo log: record only pairs newly
added after a candidate begins, remove those additions on failure, and retain
them on success. A layered trial set that consults the established set and merges
only new proofs on success is equivalent. Either shape makes rollback cost
proportional to the work performed by that candidate rather than to every proof
established before it.

Maintain both kinds of regression:

- retain the current four-entry correctness witness; and
- add an allocation-growth test like the project's existing admission and
  resolver cost tests, comparing reverse-ordered structural-key maps at two
  sizes. The ratio, rather than a stopwatch, should reject cubic copying while
  remaining insensitive to machine speed.

## Explicitly open scope

The ruling's type-layer decision is unchanged. The ledger correctly records
“`[]` is always a list” and its expected-type successor, but `[]` cannot yet
become `Lookup.Empty` from an expected lookup type. This is not a regression in
`adef7d7`; it remains an acknowledged Section E completion blocker.

## Safeguards that now hold

- The original memo-poisoning maps compare unequal in both directions.
- Those unequal maps are accepted as two distinct compound keys.
- Equal maps written in different orders still compare equal.
- The primed graph test now demonstrates the designer-approved cutoff trade
  rather than lazy first evaluation.
- Lookup order, miss behavior, Fault propagation, unknown-kind refusal, Error
  key refusal, structural indexing, depth, DAG sharing, and group invariants
  continue to pass their focused tests.
- The ruling and amended dispositions are committed, and the superseded miss
  section is visibly labelled where it appears.

## Verification record

- Inspected the complete `b98c550..adef7d7` change and adjacent equality,
  admission, indexing, cutoff, documentation, and tests.
- Focused Release aggregate/lookup/indexing/expiry suite: **68 passed**.
- Full Debug suite: **1,223 passed, 0 failed, 0 skipped**.
- Full Release coverage suite: **1,223 passed, 0 failed, 0 skipped**.
- Locked restore: passed.
- Warning-as-error Release build: passed with zero warnings and zero errors.
- Release coverage: **3,737/3,737 lines** and **2,575/2,575 branches**
  (100% each; method coverage also 100%).
- Direct plus transitive package vulnerability audit: no known vulnerable
  packages in the configured source.
- `git diff --check`: passed before this report was added.

The allocation probes were temporary and were removed after measurement. The
working tree was clean before this report; this report is the only artifact added
by the reassessment.
