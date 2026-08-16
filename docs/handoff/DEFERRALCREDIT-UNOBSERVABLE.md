# Corrected: the displacement cap IS observable

> **Ledger** — `[V]` Corrected: the displacement cap IS observable
> supersedes: DEFERRALCREDIT (the offer)
> superseded by: none

**This document originally claimed the opposite.** It said the displacement
counter was stricter than anything a program could detect, listed three shapes
that failed to observe it, and offered to remove the counter and its clamp. The
auditor found the shape in `REAUDIT21` and it is neither exotic nor far from what
I tried. The offer is withdrawn, the counter stays, and the regression I deleted
is restored in a form that works.

## The shape, and the condition I kept missing

**Shut the wait while the head is being re-armed.**

Every attempt I made left it open, so in the gap between the two head firings the
tail got its turn and drained the run that was paying. A drained run pays for
nothing after it — which is a rule I had implemented and tested one round
earlier — so the second displacement was charged for that reason, and the counter
never had to be the thing deciding.

With the wait shut, the same run is still parked when the head fires again, and
the counter is the only thing that can refuse it:

1. a two-position chain parks one real run at a shut wait, in a step of its own;
2. the head body shuts its own trigger and the wait, and changes `tick`;
3. a `Changes` driver on `tick` re-opens both together;
4. the driver's round sees the wait shut, so nothing drains; the next round has
   the head and the tail ready together, and the head displaces the same run
   again.

At `cascades: 3`, measured both ways:

| | head bodies run | diagnostic |
|---|---:|---|
| with `--credit.Displacements` | 2 | `3 rounds counted against the limit, out of 4 in all` |
| without it | 3 | `3 rounds counted against the limit, out of 6 in all` |

A body executes one more time. That is not an accounting difference visible only
to the scheduler — it is an author's `when` body running, with whatever it does
to the world, because a counter said the step had already been forgiven for it.

## What was wrong with my reasoning

Not the individual measurements — those were right, and each of the three shapes
really does fail to observe the cap. What was wrong was the step from "three
shapes failed" to "it is unobservable".

I had even written the caution and then not applied it: *"I could not construct
one" is not "none exists"*. Having said that, I still let the conclusion stand in
the title, in the summary, and in an offer to delete working code on the strength
of it. A disclosure that hedges in its body and asserts in its heading is read by
its heading.

The specific blind spot is worth naming because it is mechanical: all three
attempts varied *who fires* — a phaser, a third position, a self-arming chain —
and none varied *what is open*. The condition that mattered was a guard, not a
schedule. When an enumeration keeps coming back negative, the axis it does not
vary is the one to look at, which is the same lesson `Q2SETTLED.md` §1 drew from
the designer's two-way enumeration of what a round consumes.

## What is in the tree now

`AndOneParkedRunForgivesOneDisplacementNotEveryOneAfterIt` in
`Test/Unit/Waiting.cs`, asserting the head body ran exactly twice and pinning the
`3 counted / 4 in all` report. It fails with `expected 2, actual 3` when the
decrement is removed, which is the guard the suite had been missing since
`REAUDIT20`.

The `2k` bound in `docs/spec/grammatical-structure.md` therefore stands as a
maintained rule with a test behind it, rather than as an aspiration.

The rest of that earlier table — the eight other credit tests and what breaks
each — was accurate and still holds.
