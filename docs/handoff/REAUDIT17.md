# Re-audit 17 — `REAUDIT16` incorporation

**Audited:** `638e7c1` through `839c2a8`

**Date:** 2026-08-02

## Result

This is not yet a sign-off. The stopped-chain crash, malformed type-`when`
crash, ordinary deferred-tail delay, injection duplication, and all-chain
resting scan are genuinely repaired. Three scheduler interactions remain:

1. a newly active chain can fault after successfully running and publishing a
   continuation;
2. deferred inherited work is still rejected when the round budget is exhausted
   in the round that deferred it; and
3. a failed continuation can be retried several times in the same step merely
   because an unrelated reaction keeps that step moving.

The first is a release blocker because it affects the settled, ordinary
`wait until true` path and violates the runtime's transactional fault message.
The second is the precise round-limit interaction the previous report warned
about. The third is the thin failure-retry policy the programmer called out.

## Findings

### 1. Release blocker: a chain activated from rest has no quota entry, so an immediately satisfied wait faults after its writes were published

The sparse-chain repair builds `quota` only for chains in `active`
(`Compiler/Runtime/Graph.cs:671-678`). A chain at rest is correctly absent at
the beginning of its activation step. Its head later adds the chain to `active`
and stages its first count (`Graph.cs:419-422`), but that does not add the
chain's counters to the already-built quota table.

If the first wait is already satisfied, its continuation runs later in the same
step. `Consumed()` indexes `quota[consuming]` under the old invariant that every
counter was inserted at step start (`Graph.cs:1407-1412`). The counter is
missing, so it throws `KeyNotFoundException`.

The focused probe used only the real path:

```csharp
Graph graph = new();
graph.Var("a", false);
graph.Var("result", 0d);
graph.Chain("chain",
            (scope => scope.Read("a"), _ => { }),
            (_ => true, scope => scope.Write("result", 1d)));
graph.Prime();
graph.Write("a", true);
graph.Step();
```

The settled result superficially looks correct: `result` is 1 and the waiting
count is zero. But `graph.Faults` contains:

```text
«chain (after wait 1)» failed and none of its writes were applied:
KeyNotFoundException: The given key 'chain (waiting at 1)' was not present ...
```

That message's atomicity claim is also false. `Fire()` copies `staged` writes
into `pending` **before** calling `Consumed()` (`Graph.cs:845-850`). The catch
records a fault but does not retract those published writes. Consequently this
path says none of the writes applied while applying all of them.

This is not limited to a chain's first lifetime. `Draining()` removes a chain
from `active` whenever it returns to rest, so every later activation whose wait
is immediately true reaches the same missing-quota state.

The existing “wait whose condition is already true” test checks the bodies that
ran but not `Faults`, which is why the visible behavior let this pass at 100%
coverage.

**Recommendation:** a counter absent from the step-start quota has an inherited
quota of zero; that is normal after sparse activation, not a broken invariant.
Represent that explicitly rather than indexing. Complete all bookkeeping that
can fail before publishing `staged` writes, so the transaction boundary remains
true even if a later invariant regresses. Extend the already-true wait test to
assert an empty fault list, the author's committed write, and a zero count.

### 2. High: reaching the round limit in the round that creates `deferred` work still rejects a finite inherited continuation

The separate `deferred` set correctly keeps owed work in the loop condition,
but the loop is still gated by `counted < limit` before that work is allowed to
run (`Compiler/Runtime/Graph.cs:692`). If a round consumes the final budget unit
and defers an inherited position, the next iteration is refused and the
post-loop check throws because `deferred` is nonempty (`Graph.cs:713-716`).

This was reproduced through the real chain path with `cascades: 2`; no generated
counter was seeded by hand:

1. A normal head activation parks one run at a false wait.
2. A separate `starter` reaction is triggered in a later step.
3. Round 1 runs `starter`, which stages the head condition, wait condition, and
   a `bail` value. This is the first counted round.
4. Round 2 makes both the head and the inherited tail ready. Declaration order
   runs the head; it returns, so the already-waiting tail is deferred. This is
   the second counted round.
5. The tail is finite work inherited from a previous step, but the loop stops at
   the limit and throws `RunawayCascade` with last fired `«starter», «chain»`.

The tail is not newly created work and would consume its step-start quota if it
were allowed to run. This is precisely the case for which the previous audit
said that adding another loop condition was insufficient without revisiting the
limit gate and post-loop invariant. Giving the work a separate set does not by
itself give it a chance to demonstrate that it is exempt progress.

The programmer's note mentions existing `cascades: 1` boundary tests, but the
current repository contains no `cascades: 1` construction in `Test/`; the
explicit runtime configurations present are 2, 4, 16, and defaults. The new
same-step deferred test uses the default limit and therefore cannot reach this
boundary.

**Recommendation:** settle how already-owed deferred positions are admitted
after the counted budget reaches its boundary. The scheduler must be able to
run finite inherited work without also granting unlimited newly-created work;
the per-counter quota already knows which consumption is inherited, but the
current loop decides before that fact can be observed. Add the real-path
two-round sequence above and an actual limit-one boundary test if limit one is
intended to exercise chains. Merely raising the test's limit masks the
accounting defect.

### 3. Medium: failed continuation retry is queued in the current round set, so unrelated pending work can run the same failed body repeatedly in one step

On any body exception, `Fire()` adds the trigger directly to `woken`
(`Compiler/Runtime/Graph.cs:855-863`). `Triggered()` clears and consumes
`woken` every round, not once per step. Therefore “retry later” currently means
“retry in the next cascade round if anything else happens to keep this step
alive.”

The focused probe parked one run, made its wait true, and used an unrelated
finite `Changes` reaction to keep two additional rounds pending. The
continuation body incremented an external attempt count and threw. One call to
`Step()` produced **three attempts and three faults**, rather than one attempt
and one future retry. With no unrelated pending write, the same failure is tried
once, demonstrating that unrelated graph activity controls retry count.

This is not only wasted deterministic work. A body may fail after an effect
that cannot be rolled back; repeating it before any new step, code edit, or
relevant state change can duplicate that effect. It also contradicts the
nearby authoritative explanation that an effect body “cannot simply be run
again” (`Graph.cs:823-833`).

The existing failed-body test covers the catch branch while an intentional
spin reaches `RunawayCascade`, but it does not count attempts or faults and so
does not pin the retry policy.

**Recommendation:** keep failed-continuation retries in a next-step queue rather
than the current round's dirty wake set. Swap them into candidates at the next
`Step()` boundary, or state and test a different policy explicitly. Add a
cross-chain pending-work regression; a single quiet failure step cannot reveal
the distinction.

### 4. Low: a few authoritative comments still contradict the code, including two at the new fault boundary

- `Compiler/Runtime/Graph.cs:13-17` says `TriggerMode` has “both cases” and is
  never level-triggered, while the enum has three cases and `WhileTrue` is
  explicitly level-triggered.
- `Graph.cs:823-833` says an effect body cannot simply be run again, while the
  new catch explicitly requeues it and can run it again in the same step.
- `Graph.cs:1407-1412` says quota is rebuilt from every counter and a miss is a
  defect. Sparse activation deliberately omits resting counters, and that stale
  invariant directly causes finding 1.
- `Graph.cs:1363-1364` has two consecutive XML `<summary>` elements on
  `returned`, one left from the old field.

These are not the project's settled hand-alignment style. The quota and retry
comments describe invariants that the new implementation no longer has and
should be corrected as part of the corresponding fixes.

## Status of the six `REAUDIT16` findings

| prior finding | result |
|---|---|
| 1 — stopped chain leaves stale deferred names | **Fixed.** Every scheduler index is cleared and candidates are filtered before sorting. The three-segment regression is the right shape. |
| 2 — malformed type `when` crashes | **Fixed.** Parse-error nodes are excluded and both malformed productions now produce `Malformed`. |
| 3 — deferred continuation falls out of its step | **Partial.** The default-limit case is fixed; exhausted-budget deferred work still throws (finding 2). |
| 4 — inactive chain scan/allocation | **Partial.** Resting scans are removed and allocation is flat, but sparse quota initialization breaks an immediately active continuation (finding 1). |
| 5 — shadow injection remains duplicated | **Fixed.** Resolver and runtime consume `Injection.Shadow`, with a direct join invariant. |
| 6 — superseded terminology | **Mostly fixed.** The named `return`, count, and fungibility corrections are real; the remaining contradictions are finding 4. |

## Verification

- Temporary focused probes reproduced findings 1 through 3 and were removed.
- Debug: **855 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **855 passed**, with **100% line, branch,
  and method coverage**.
- Source/test `git diff --check 638e7c1..839c2a8`: clean.
- The worktree was clean after probe removal and before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding. Formatting was not used as a gate.

The previously disclosed feature and pipeline work remains outside this
re-audit and is not repeated as findings.

## Recommended order

1. Treat a newly active counter as zero inherited quota and restore the true
   transaction boundary before publishing staged writes.
2. Redesign the round-limit admission of already-deferred inherited work and
   pin the real-path budget-boundary sequence.
3. Separate next-step fault retries from current-round dirty wakes.
4. Correct the stale scheduler and quota documentation while changing those
   paths.
