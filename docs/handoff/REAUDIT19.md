# Re-audit 19 — `REAUDIT18` incorporation and Q2 settlement

**Audited:** `dee02ef` through `84f3712`

**Date:** 2026-08-02

## Result

This is not yet a sign-off.  The graph-wide deferral pool from `REAUDIT18` is
gone, the cross-chain reproduction now fails at six charged rounds and settles
at seven, the cap is exercised at a positive boundary, and the final two-credit
decision is represented in both the maintained spec and a non-vacuous test.
Those repairs are real.

One high-severity ownership defect remains inside a single wait.  The new
allowance is owned by the right *position*, but it outlives the inherited run
that supplied it.  If that run is consumed before being displaced, its unused
deferral allowance can later forgive a newly created run at the same position.
This is the cross-generation form of the subsidy that the cross-chain repair
removed, and it contradicts the settled invariant that `cascades` counts every
round spent on work created during the step.

There is also one low-severity diagnostic mismatch made visible by the new
accounting rule.

## Findings

### High: a drained run's unused deferral allowance pays for new work at the same wait

At step start, `Step()` reads each wait count into two independent tables
(`Compiler/Runtime/Graph.cs:690-701`):

```csharp
quota[chain.Counts[wait]] = owed;
allowance[chain.Reacting[wait + 1]] = owed;
```

Consumption spends only `quota` (`Graph.cs:1503-1513`).  Deferral spends only
`allowance` (`Graph.cs:1477-1489`).  Consequently consuming the last inherited
run leaves its full unused deferral allowance behind.  `Throttled()` checks only
whether the deferred position has allowance; it never checks whether any
inherited run is still parked there.

That is not the two-credit rule settled in `Q2SETTLED.md`.  Two credits means an
inherited run may pay once for **its** displacement and once for **its** drain.
It does not mean that its displacement credit survives its drain and may be
transferred to a replacement created later.  The authoritative spec is
explicit: the exempt round is one in which inherited work is "being displaced
by the head that owns it" (`docs/spec/grammatical-structure.md:291-294`).

#### Full-path reproduction

The temporary probe used only ordinary `Graph` declarations, writes, and
steps—no hand-seeded counters:

1. A two-position chain parks one real run at a shut wait in an earlier step.
2. The tested step opens the wait, so round 1 drains that inherited run.
3. A finite `Changes` reaction then arms the head, lowers it, and raises it with
   the wait open.  These rounds create two new runs at the same wait.
4. When the head and continuation become ready together, declaration order runs
   the head and defers the continuation.  Every run at that wait was created in
   the current step; the inherited run was already consumed in round 1.

At `cascades: 5` the rounds are:

| round | work | correct charged total | current charged total |
|---:|---|---:|---:|
| 1 | drain the sole inherited run; begin finite phasing | 0 | 0 |
| 2 | head creates a new run; phasing lowers it | 1 | 1 |
| 3 | phasing raises the head and opens the wait | 2 | 2 |
| 4 | head creates another run; the first new continuation is deferred | 3 | **2** — paid by the drained run's stale allowance |
| 5 | consume one new run | 4 | 3 |
| 6 | consume the other new run | 5 | 4 |
| 7 | propagate the final decrement and settle | must not be admitted | 5 |

The correct rule exhausts the five-round budget with the final decrement still
pending and throws `RunawayCascade`.  The current implementation returns `7`.
The effect scales with queue depth: drain `k` inherited runs before creating new
ones at the same wait, and as many as `k` stale allowances can forgive
created-work deferrals.  A genuine runaway is therefore detected up to `k`
charged rounds late, directly contradicting the spec's "exactly `cascades`
rounds of created work, undelayed" guarantee (`grammatical-structure.md:296-300`).

The current controls miss this ordering:

- `AndNoOtherChainsParkedRunCanBeSpentOnIt` proves ownership across positions;
- `AndOneParkedRunForgivesOneDeferralNotEveryOneAfterIt` leaves the inherited
  run parked when it spends the allowance; and
- `AndARunPaysForItsOwnDisplacementAndItsOwnDrainBoth` displaces the run before
  draining it.

None drains first and attempts to displace newly created work afterward.

**Recommendation:** keep consumption and deferral as separate credits, but keep
them in one per-wait ownership record.  After a successful inherited
consumption, the remaining deferral allowance cannot exceed the number of
inherited runs still parked at that wait.  With fungible runs, capping the
unspent allowance by the remaining consumption quota preserves the valid
displace-then-drain case while retiring unused displacement credit on
drain-first.  The position/counter association needs to be explicit so this
cannot become another parallel-table drift.

Add a real-path boundary test for the sequence above: five must throw, six must
settle in seven physical rounds.  Keep the existing inverted Q2 test as the
control that displacement before consumption still gives both exemptions.

### Low: the runaway diagnostic describes charged rounds as every physical round

`Runaway()` still says:

> Every round created the work for the next

and its comment says that this is now the only way to reach the exception
(`Compiler/Runtime/Graph.cs:1009-1024`).  That stopped being true once inherited
consumption and owned displacement rounds became explicitly free.  A step may
service inherited work for several physical rounds, then enter a genuine
created-work runaway and throw after the configured number of *charged* rounds.
The exception reports the larger physical `rounds` value and then says every one
of those rounds created work, sending the author back toward precisely the
misdiagnosis the comment says it intends to avoid.

**Recommendation:** pass `counted` to `Runaway()` as well.  State that the graph
exhausted `cascades` charged rounds (and optionally how many physical rounds it
took), and describe the last firings as likely sources rather than claiming
that every physical round created its successor.

## Status of the `REAUDIT18` finding and design settlement

| item | result |
|---|---|
| cross-chain graph-wide pooling | **Fixed.** Allowance is keyed by continuation position, and the six/seven-round reproduction is pinned. |
| positive cap rather than a zero boundary | **Fixed.** One parked run is tested at `N` and `N + 1`; the zero case is accurately retained as the no-credit/hang control. |
| Q2: displacement and drain are separately free | **Implemented.** The test has a real parked run and fails if the credits are collapsed. |
| same-position ownership across time | **Not fixed.** Position ownership prevents cross-chain transfer but not transfer from a drained generation to a new one. |

No additional defect was found in the cross-chain repair or Q2 settlement.

## Verification

- A temporary full-path probe reproduced the same-position credit leak and was
  removed.  At `cascades: 5`, the implementation returned seven rounds where
  `RunawayCascade` was required.
- Debug: **864 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **864 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check dee02ef..84f3712`: clean.
- The worktree was clean after probe removal and before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.

The previously disclosed feature and pipeline work remains outside this
re-audit and is not repeated as findings.

## Recommended order

1. Retire an inherited run's unused deferral entitlement when that run drains,
   while preserving the settled two-credit displace-then-drain path.
2. Add the drain-before-new-deferral five/six-round boundary control.
3. Make `RunawayCascade` distinguish charged rounds from physical rounds.
