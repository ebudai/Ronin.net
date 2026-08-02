# Re-audit 18 — `REAUDIT17` incorporation

**Audited:** `839c2a8` through `dee02ef`

**Date:** 2026-08-02

## Result

This is not yet a sign-off. The sparse-activation quota fault and false
transaction message are fixed, and failed continuations now retry once per step
rather than once per unrelated cascade round. The positive deferred-tail
boundary from `REAUDIT17` also passes.

One high-severity accounting defect remains in the new semantic rule: deferral
forgiveness is capped by a **graph-wide total** of inherited runs, but is not
attached to the run, wait, or chain actually deferred. An inherited run parked
in one chain can therefore pay for a newly created run deferred in another.
That directly contradicts the new authoritative wording and recreates the
cross-chain pooling shape that per-counter quota removed.

The programmer is right that whether deferral should receive any exemption is
a language/runtime design decision. The implementation defect below is
independent of that decision: even if the new rule is approved, the code does
not enforce the rule it documents.

## Finding

### High: graph-wide deferral credit lets an unrelated inherited queue forgive newly created work in another chain

At step start, quota is correctly recorded per counter
(`Compiler/Runtime/Graph.cs:678-687`). The new deferral allowance immediately
collapses that information to one number:

```csharp
var inherited = quota.Values.Sum();
var throttled = 0d;
```

After each round, any nonempty `deferred` set receives forgiveness while that
global number has credit (`Graph.cs:692-697,728-742`):

```csharp
var throttling = deferred.Count is not 0 && throttled < inherited;
```

Nothing asks which continuation was deferred, which counter it would consume,
or whether that counter had any step-start quota. Consequently “the graph
inherited some run somewhere” is treated as “this deferred run was inherited.”

That is weaker than both maintained statements:

- `docs/handoff/README.md:54-55` says a round is free when it defers **a run the
  step inherited**; and
- `docs/spec/grammatical-structure.md:270-285` distinguishes inherited work from
  work created during the step and says the latter is what the limit counts.

#### Real-path reproduction

The temporary probe used two ordinary chains and no hand-seeded counters:

1. `old chain` parks one run at a false wait in an earlier step. It is the only
   inherited run and never becomes ready during the tested step.
2. `new chain` begins at rest. A finite starter cascade creates its first run,
   lowers and re-raises its head, and raises its wait guard—all during the tested
   step.
3. The re-raised head and the new continuation are ready together. Declaration
   order runs the head and defers the continuation.

With `cascades: 6`, the tested step is:

| round | work | correct charge | current charge |
|---:|---|---:|---:|
| 1 | starter raises `new head` | 1 | 1 |
| 2 | new head creates count 1 and moves phase | 1 | 1 |
| 3 | phase reaction re-raises head and wait guard | 1 | 1 |
| 4 | new head creates count 2; new tail is deferred | 1 — the tail's run was created this step | 0 — paid by `old chain`'s unrelated inherited run |
| 5 | new tail consumes one new run | 1 | 1 |
| 6 | new tail consumes the other new run | 1 | 1 |
| 7 | final decrement lands and the chain settles | requires the seventh charged round | admitted as charged round 6 |

Under the documented rule, the step reaches its six-round budget with a pending
decrement and throws `RunawayCascade`. The current implementation does not
throw. The only difference is the unrelated parked run in `old chain`.

This is bounded, but the bound can be arbitrarily large: a healthy inherited
queue may contain thousands of runs. It can therefore delay runaway detection
by thousands of newly-created deferral rounds in unrelated chains—the exact
kind of cross-chain subsidy the quota was made per-counter to prevent.

#### Why the new cap test does not cover the cap

`AndItBuysNoMoreOfThoseThanItInheritedRuns`
(`Test/Unit/Waiting.cs:1087-1119`) creates no run before its tested step.
Therefore `active` is empty at step start, `quota.Values.Sum()` is zero, and
`throttling` is never true. The test proves only that zero inherited runs buy
zero forgiveness; it does not exercise a positive cap, ownership, or exhaustion
of that cap.

**Recommendation:** preserve ownership through deferral. Map each generated
continuation to the counter it consumes and maintain a per-counter deferral
allowance initialized from that counter's step-start quota. A round may be
forgiven only if at least one position it actually deferred has allowance; spend
that allowance from the corresponding counter. Do not reduce the quota to a
graph-wide sum.

Add three tests:

1. the current positive case: an inherited continuation deferred by its own
   head is forgiven;
2. the negative cross-chain case above: an unrelated inherited run cannot
   forgive a new continuation; and
3. a genuine positive-cap exhaustion case with `N > 0` inherited runs and more
   than `N` qualifying deferrals. The present zero-cap test cannot stand in for
   this.

## Design decision still requiring confirmation

The new spec now says deferring inherited work is uncharged. That resolves the
budget-boundary example, but it is a new scheduler rule rather than a mechanical
repair. Before treating it as settled, the designer should explicitly answer:

1. Is the exemption owned by the specific run/counter deferred, as the rest of
   quota accounting is, or is any aggregate cap intended? The maintained README
   currently says the former.
2. May one inherited run forgive both a deferral round **and** its later
   consumption round? The implementation uses a separate `throttled` counter and
   does not spend consumption quota on deferral, so it currently permits both.
3. May the same inherited run be deferred repeatedly and forgive multiple
   rounds, provided other inherited runs leave aggregate credit, or is each run
   entitled to at most one deferral exemption?

A per-counter implementation makes these choices explicit. The current global
sum silently answers all three and permits cross-chain payment.

## Status of the four `REAUDIT17` findings

| prior finding | result |
|---|---|
| 1 — newly active chain faults after publishing writes | **Fixed.** Missing quota now means zero inherited work, bookkeeping precedes publication, and the already-true wait test checks faults, writes, and count. |
| 2 — exhausted-budget deferred inherited work | **Partial.** The positive boundary passes under the new exemption, but ownership is lost and unrelated inherited work subsidizes new deferrals. |
| 3 — failed continuation retries repeatedly in one step | **Fixed.** `stalled` is moved to `woken` only at the next step boundary, and the cross-chain retry-count test pins it. |
| 4 — stale scheduler comments | **Fixed.** Trigger mode, retry, quota, and duplicate-summary prose now match the implementation. |

No additional defect was found in the other three repairs.

## Verification

- A temporary full-path probe reproduced the cross-chain subsidy and was
  removed.
- The four focused incorporation tests pass.
- Debug: **858 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **858 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 839c2a8..dee02ef`: clean.
- The worktree was clean after probe removal and before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding. Formatting was not used as a gate.

The previously disclosed feature and pipeline work remains outside this
re-audit and is not repeated as findings.

## Recommended order

1. Ask the designer to confirm the deferral-exemption semantics above.
2. Regardless of that answer, remove graph-wide pooling and associate any
   approved forgiveness with the deferred counter that owns it.
3. Replace the zero-cap test with positive ownership and exhaustion controls.
