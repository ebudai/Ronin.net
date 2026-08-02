# Deferral credit — the falsifying test was run

Answering `DEFERRALCREDIT.md`. The auditor's implementation defect is fixed
independently: the credit is owned per counter, `quota.Values.Sum()` is gone, and
there are tests either side of it.

The prior question was the right one to ask first, so it was run first.

## §0 — the exemption survives, and the enumeration missed a case

> A round that defers an inherited continuation must have run *something*
> instead: it consumed other inherited work, or it consumed newly created work.

There is a third: **it consumed nothing.** A round that fires a chain's *head*
runs a producer, not a consumer — and in the `REAUDIT17` sequence the head then
called `return`, so that round consumed nothing and created nothing either. It
was charged, it spent the budget, and the step threw with an inherited run still
owed. That is the case the two-way enumeration has no room for.

### The measurement

A chain with `k` runs parked at a shut wait. One step opens the wait and re-arms
the head `k` times, so the tail is displaced `k` times. No hand-seeded counters;
the runs are parked by ordinary steps.

The work is identical in both columns — 5, 8, 11 and 17 rounds respectively. Only
the charge differs:

| parked runs | least `cascades` with the exemption | without it |
|---:|---:|---:|
| 1 | 3 | 4 |
| 2 | 4 | 6 |
| 3 | 5 | 8 |
| 5 | 7 | 12 |

Without the exemption the budget a healthy queue needs grows at **twice** the
rate: `2k + 2` against `k + 2`. That is the same defect the consumption rule was
written to remove —

> Without that qualifier a queue deeper than the limit could never drain, which
> made it an accidental cap on how deep a chain could get.

— arriving one level up. Deleting the exemption reinstates an accidental cap on
queue depth, just with a factor of two instead of a factor of one.

So §1 applies.

## Q1 — owned by the counter. Agreed, and done

The allowance is per position, initialised from the count parked at that position
when the step began. A count and the continuation waiting on it are one to one,
so what forgives a round is a run parked at the very wait the round declined to
serve.

The auditor's cross-chain reproduction is now a test either way round: at
`cascades: 6` it throws, at 7 it settles in 7 rounds, and the only difference
between the two graphs is a parked run in a chain that never runs.

## Q3 — at most one per run. Agreed, and done

One run forgives one deferral at its own wait. A second deferral of that
position, with nothing left parked there, is charged. There is a test at `N` and
`N + 1` rather than at zero: one parked run, its wait opened during the step, its
head re-armed twice — `cascades: 4` throws and 5 settles.

## Q2 — "one credit, spent on whichever comes first" was measured, and it is a no-op

This is the one to look at again.

Under it, a run's deferral and its consumption draw on the same credit. Running
the same measurement:

| parked runs | one credit, whichever first | no exemption at all |
|---:|---:|---:|
| 1 | 4 | 4 |
| 2 | 6 | 6 |
| 3 | 8 | 8 |
| 5 | 12 | 12 |

**Identical.** The arithmetic is why: `k` runs generate `k` deferrals and `k`
consumptions, so demand is `2k` against a supply of `k`, and half the rounds are
charged however they are ordered. Unifying the credit removes exactly as much as
deleting the exemption removes, in exactly the case that motivated having one.

So the choice is not between one credit and two. It is between two credits and
none.

The argument in the question stands as an argument — one unit of pre-existing
work buying two rounds does dilute what `cascades` means — but the remedy costs
the whole mechanism. If the dilution is the real objection, the honest fix is to
say so in the number: a step that inherits `k` runs is allowed `limit + 2k`
rounds, and that is a documented property of the limit rather than a hidden one.

The fourth test suggested in §2 — "one inherited run, deferred once and consumed
later in the same step; exactly one of those two rounds is free" — is the test
that encodes Q2, so it is not written. Under the current design both are free. It
is a one-line test to add the moment Q2 is settled either way.

## §2 — the generalisation is applied

> A boundary test placed at zero does not exercise the boundary.

Correct, and it was true of the test as written. It is renamed to what it
actually proves — a step that inherited nothing is forgiven nothing — and kept,
because it is also the hang test: without any cap it does not fail, it runs for
ever. The positive cases are the two new tests above, at `N` and `N + 1`.

## Where it is

- `Compiler/Runtime/Graph.cs` — `Throttled()`, and the allowance built beside the
  quota in `Step()`
- `docs/spec/grammatical-structure.md` §4.5.5 — the round-limit paragraphs
- `Test/Unit/Waiting.cs` — "a round that deferred work did not fail to settle",
  "and a step that inherited nothing is forgiven nothing", "and no other chain's
  parked run can be spent on it", "and one parked run forgives one deferral, not
  every one after it"
