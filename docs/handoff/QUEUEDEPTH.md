# The detector is in. §1's premise is not — a deep queue cannot drain

> **Ledger** — `[R]` The detector is in. §1's premise is not — a deep queue cannot drain
> supersedes: not yet checked
> superseded by: not yet checked

`ACCUMULATION-BOUND.md` implemented, and the two things §5 asked to be measured
were measured. One of them turned up something that changes §1.

---

## 1. What was asked for, and what it does

The low-water-mark detector is built. A chain's pending runs are summed once per
step, the minimum over a window of `K` steps is kept, and when one window's
quietest moment is busier than the last's, it reports.

**§5's two measurements.**

| | result |
|---|---|
| false positives on a slow drain | none — a queue 40 deep, filled and emptied six times, reports nothing |
| cost | 20 ms per 20,000 steps, about 1 µs/step for everything including the detector. It is one sum and one comparison per chain per step |

`K` is 256. That is picked, not derived, and it is still picked — see §4.

The teaching message is the one from §3, delivered as a fault.

## 2. What the measurement turned up instead

Trying to build the *false-positive* case — a queue that takes longer than a
window to drain — I could not, because the program dies first:

```
queue of  63  →  drained 63, still pending 0
queue of  64  →  RunawayCascade
```

**Runs are taken one per round, so draining `k` of them needs `k` rounds inside
one step — and the round limit is 64.** The limit that exists to catch a `when`
scheduling itself is therefore also a cap on how deep any chain may ever get,
and it was nobody's decision.

Two consequences.

**§1's example cannot happen.** *"«when order placed { reserve; wait until
payment cleared; ship }» with 10,000 pending is a Tuesday"* — it is not. At 64 it
throws when the payments clear. So the two programs in §1 are not structurally
identical after all: the busy queue does not survive being busy, and the argument
that depth cannot separate a leak from a queue is true for a narrower reason than
stated — every queue is shallow, whether it wants to be or not.

**The diagnostic blames the wrong thing.** What the author gets is

> the graph did not settle after 64 rounds … A when body is writing a var its own
> trigger reads, so every firing schedules the next. Stop the body writing once
> the condition it acts on is satisfied.

which is a correct description of a different program. Nothing was writing what
its trigger reads; a queue drained.

## 3. Why this is the same error class again, one level down

`ACCUMULATION-BOUND.md` §0 named it: *a checker's description of what it catches
is a coverage claim needing its own adversarial test.* The round limit says it
catches self-scheduling `when`s. It also catches ordinary draining, and nothing
had asked it whether it could tell the two apart.

I did not find this by reasoning about the detector either. I found it trying to
construct a false positive and failing.

## 4. What I have not decided, because it is not mine to

The interaction is between one-run-per-round and the round limit, and both are
load-bearing for different reasons — the first so several runs do not write the
same cells in one settle, the second so a cascade cannot spin. Options I can see,
none costed:

- **raise the limit** — cheapest, and it only moves the wall;
- **do not count draining rounds against it** — a round that only advanced chains
  is not a cascade, though telling them apart needs the loop to know why it ran;
- **take more than one run per round when the runs write disjoint cells** — the
  original reason for one-per-round was write collision, which is a property of
  the segment, not of the count;
- **accept a shallow cap and say so** — with a diagnostic that names the real
  cause, which is the minimum whatever else is chosen.

`K` should be chosen after this, not before. Its job is to be longer than a
realistic drain, and right now nothing can drain more than 63 runs at all — so
any `K` I pick is fitted to a bound that may be about to move.

## 5. Provenance

Run at `04c864e` plus the detector:

- 63 drains, 64 throws — the boundary is exact and reproducible;
- the leak case reports twice over three windows, which is right: the first
  window has nothing to compare against;
- the slow-drain case reports nothing over six fill-and-empty cycles;
- 20,000 steps in 20 ms with the detector running.

All four are now tests rather than probes, including the depth cap, which is
recorded as behaviour rather than asserted as correct.
