# Re-audit 48 — ruling incorporation and unordered lookup equality

**Re-audited:** `80accb4..b98c550`, including the implementation of
`REAUDIT47RULING.md` and the current working-tree ruling/amendments.

**Date:** 2026-08-12

## Result

**No sign-off. One high-severity runtime correctness defect and two medium
safeguard/integration findings remain.** Section E's type layer also remains the
explicitly ruled open scope; it is recorded in the expiry ledger but is not
implemented.

The ruling's main changes are otherwise incorporated correctly. Lookup storage
preserves insertion order, canonical sorting and its display fallback are gone,
duplicate detection asks language equality against prior keys, unknown host
kinds are refused recursively, CLR null becomes a `Fault`, ordinary errors are
legal values but not keys, lookup misses return `nothing`, and the expiry ledger
names expected-type construction of the empty lookup as the successor to the
temporary “`[]` is always a list” rule.

The broader Fault propagation disclosed by the programmer is sound. A Fault is
the one failure that is not a value; placing it inside a list or lookup would
still turn it into part of a value. Returning it unchanged from an enclosing
admission therefore follows the ruling's general rule rather than exceeding it
in a conflicting direction. I found no second failure-to-value conversion that
launders a Fault.

The disputed cutoff result is also resolved. The original FRESHAUDIT20 result
still reproduces when the graph is primed: after `first key` has cached `"a"`, a
reorder-only recompute changes the directly walked table to `"b"` but leaves
`first key` at `"a"`. That is now behaving as designed. The new maintained test
gets `"b"` for a different reason: it never reads `first key` before the write,
so the table has never run, no dependency edge exists, and `first key` is first
evaluated only after `reverse` is already true.

The new runtime defect is in the memo carried through unordered lookup matching.
A failed candidate comparison leaves pairs recorded as “proven”; a later
candidate can reuse one and call unequal aggregate keys equal. The temporary
probe produced `Builtin.Same(left, right) == true` for two lookups with different
key sets. This affects duplicate-key admission, indexing's equality relation,
and graph cutoff — the exact consumers the ruling requires to share one honest
equality.

## Disposition of REAUDIT47 and its ruling

| Prior item | Re-evaluation |
|---|---|
| Canonical comparer incompatible with equality | **Closed by deletion.** No runtime total-order path remains. |
| Canonical comparison exponentially unfolds DAGs | **Closed by deletion.** The sorter and comparer are gone. A separate correctness defect exists in the equality memo; finding 1. |
| Fault key is laundered into Error | **Closed.** Direct and nested Faults leave admission as Faults. The disclosed aggregate-wide propagation is accepted. |
| Type layer and expected-type empty lookup | **Open by explicit ruling.** The required expiry-ledger entry exists. This remains outside any “Section E complete” claim. |
| Lookup miss contradicts the ruling | **Closed.** A miss returns the `Nothing` singleton; list out-of-range remains Error. |
| FRESHAUDIT20 finding 1 | **Behaving as designed.** The original primed stale-dependent result is accurate. The replacement maintained test does not reproduce it; finding 2. |

## Findings

### 1. A failed unordered-key candidate poisons the equality memo

**Severity: high — unequal lookup values can compare equal, so cutoff can
suppress a real value change and distinct compound keys can be refused as
duplicates.**

`Builtin.Same` uses one `HashSet<(object, object)>` across all nested aggregates
(`Compiler/Runtime/Values.cs:344-376`). List and lookup comparisons insert their
pair before proving it (`Values.cs:392-400, 445-455`). That is safe on a
straight-line comparison: a mismatch returns from the whole public comparison
and the memo is discarded.

Unordered lookup matching adds backtracking. For each key, it tries every key on
the other side (`Values.cs:398-413`). When a candidate differs late, the code
continues to the next candidate but retains every aggregate pair inserted while
the failed candidate was explored. Encountering one of those pairs later hits
`proven?.Add(...) is false` and returns true without comparing its contents.
The comment at `Values.cs:434-437` therefore does not hold on this path: a pair
met twice is not necessarily already proved; it can be residue from a candidate
that failed.

The temporary production-runtime probe used two unequal admitted lists:

```text
A = [1, 2]
B = [1, 3]

left keys  = [[A, "x"], A, [B, "x"], "filler"]
right keys = [[B, "x"], [A, "x"], B, "filler"]
```

All values were `0`. The maps differ — `left` has key `A`, where `right` has
key `B` — but `Builtin.Same(left, right)` returned **true**. Comparing the first
left key against the first right candidate records `(A, B)` and then fails.
The first key subsequently finds its real match. When `A` is compared with `B`
for the next association, the stale pair makes it pass.

The consequences are wider than `is`:

- `List.Associated` uses this result to reject duplicate keys, so two distinct
  lookup-valued keys can be refused as one key.
- `Builtin.Found` uses it for indexing, so the equality used to locate a
  compound key can report a false hit.
- `Graph.Recompute` uses it at the change cutoff (`Graph.cs:1527-1533`), so this
  false equality can suppress a genuine map change. Unlike insertion-order-only
  cutoff, this is not the designer-approved trade: the associations differ.

**Recommendation:** make memo updates transactional across candidate search.
Compare a candidate with a forked context and commit newly proved pairs only
when that candidate succeeds; discard them when it fails. Equivalently, use a
memo representation that distinguishes in-progress, proved-equal, and failed
pairs. Maintain the witness above and assert both directions are false, then use
the two maps as distinct keys of an outer lookup to guard duplicate admission.

### 2. The graph test never establishes the cutoff state it claims to test

**Severity: medium safeguard correctness — the maintained test passes while
asserting the opposite of the ruled, documented behavior.**

`ALookupIteratesAsWrittenAndComparesAsAMapWhichIsTheNamedTrade` declares `table`
and `first key`, immediately writes `reverse`, and only reads either let after
the step (`Test/Unit/LookupValues.cs:38-74`). Lets are lazy. Until one is read,
`Node.Dependencies` is empty; `Graph.Settled` expressly treats an unevaluated let
as unsettled (`Compiler/Runtime/Graph.cs:1557-1569`). There is consequently no
cached `"a"` and no edge through which cutoff could act in the maintained test.

A temporary probe added one line before the write:

```csharp
Assert.Equal("a", graph.Read("first key"));
```

After the write and step, the table's first key was `"b"` and `first key`
remained `"a"`, exactly as FRESHAUDIT20 reported and as the designer has now
classified behaving-as-designed. The probe passed. The current assertion of
`"b"` merely observes a first evaluation performed after the write; it does not
show that a dependent re-ran.

**Recommendation:** prime `first key` before writing `reverse`, then assert the
ruled pair: direct table walk `"b"`, cached dependent `"a"`. If a separate test
is wanted for lazy first evaluation, keep the current sequence under a name and
comment that describe laziness rather than cutoff.

### 3. The ruling is not part of HEAD, and live comments still prescribe sorting

**Severity: medium integration/regression risk — checking out the audited
commit loses the document that supersedes the contradictory handoff, while
nearby code comments still describe the deleted design.**

`docs/handoff/REAUDIT47RULING.md` is untracked: `git ls-files` reports that HEAD
does not know the path. The ruling intentionally leaves `EAGGREGATES2.md` §8 in
place and supersedes it from the ruling, so without that file the tracked project
again contains the old “miss is Error” handoff beside the `nothing` spec and
runtime. The amended FRESHAUDIT20 and REAUDIT47 dispositions are likewise only
working-tree edits.

Three comments adjacent to the implementation also retain the rejected design:

- `Compiler/Runtime/Values.cs:372-375` says lookup equality is sequence equality
  over a sorted canonical form, immediately before dispatching to the new
  unordered-map comparison.
- `Test/Unit/LookupValues.cs:8-12` says the fixture proves sorting makes equal
  lookups indistinguishable downstream.
- `Compiler/Runtime/Evaluator.cs:114-117` still says keys are canonicalised; in
  this context that wording is readily read as the removed canonical order.

This is especially material because the designer explicitly requested that
FRESHAUDIT20 finding 1 not remain a defect that causes sorting to return.

**Recommendation:** commit the ruling and amended dispositions with the
implementation. Replace the stale runtime and test summaries with insertion
order/unordered equality, and say keys are “admitted” or “normalised” in the
evaluator. A supersession banner in `EAGGREGATES2.md` §8 would make the current
authority visible even when that section is read directly.

## Explicitly open scope

The type layer is still absent, as the ruling acknowledges. `[]` is resolved as
a list without an expected type, and `Lookup.Empty` remains host-only. The new
entry at `Test/Expiry.cs:80-94` correctly records both the approximation and its
successor. This closes the ledger action, not the Section E type requirements;
the project should continue to exclude them from a completion claim until the
type layer lands.

## Safeguards and implementation decisions that hold

- Lookup literals retain written order, display in that order, and compare
  order-insensitively on ordinary scalar and structural examples.
- Duplicate scalar and structural keys are refused by language equality.
- Unknown host kinds are refused directly and at nested key/value/list
  positions. The language number remains `double`; a host `int` is refused.
- Null produces Fault, Fault is not caught by `otherwise`, and null in a list,
  lookup key, or lookup value propagates out of the aggregate.
- Ordinary Error remains a legal list/lookup value and is refused as a key.
- Error equality still compares exact runtime kind and reason.
- Scalar, list, and lookup keys can be indexed; a miss returns the `Nothing`
  singleton, while list bounds errors remain errors.
- The group kind/key constructor invariant remains enforced.
- Shared admitted DAGs retain one cross-kind memo on the successful-comparison
  path; finding 1 is about failed candidate rollback, not removal of sharing.

## Adversarial verification

Temporary probes were added only for execution and removed before this report.

| Probe | Result |
|---|---|
| Unequal maps with a failed aggregate-key candidate reused later | **Failed:** `Builtin.Same` returned true |
| Original cutoff graph, with `first key` primed before the write | **Passed the ruled behavior:** table walked as `"b"`; dependent remained `"a"` |
| Current maintained cutoff test | Passed, but only because its first evaluation occurs after the write |

## Verification record

- Inspected `80accb4..b98c550` and the current ruling/amendment working tree,
  including admission, equality, indexing, evaluator, graph cutoff, expiry, and
  aggregate tests.
- Focused Release aggregate/lookup/indexing/expiry suite: **67 passed**.
- Full Debug suite: **1,222 passed, 0 failed, 0 skipped**.
- Full Release suite: **1,222 passed, 0 failed, 0 skipped** on the isolated
  crash-blame rerun.
- Locked restore: passed.
- Warning-as-error Release build: passed with zero warnings and zero errors.
- Release coverage report: **3,735/3,735 lines** and **2,571/2,571 branches**
  (100% each).
- Direct plus transitive package vulnerability audit: no known vulnerable
  packages in the configured source.
- `git diff --check`: passed before adding this report.

One earlier full Release attempt reported a test-host crash in `libcoreclr`; the
kernel recorded the runtime-host segfault. It did not reproduce under the test
runner's crash-blame isolation, where all 1,222 tests passed, and the focused
affected suite was consistently green. It is recorded here as a verification
anomaly, not attributed to the lookup implementation.

The pre-existing modified audit dispositions and untracked ruling/probe files
were preserved. This report is the only project artifact added by this re-audit.
