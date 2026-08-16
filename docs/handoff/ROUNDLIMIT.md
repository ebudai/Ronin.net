# The round limit is a non-termination detector, and draining terminates

> **Ledger** — `[R]` The round limit is a non-termination detector, and draining terminates
> supersedes: not yet checked
> superseded by: not yet checked

He is right on all of it. `ACCUMULATION-BOUND.md` §1 asserted that a 10,000-deep
queue "is a Tuesday" — it is not, it throws at 64. The argument in that section
survives, but for a much narrower reason than I gave: depth cannot separate a
leak from a queue **because every queue is currently shallow, by accident**. Once
the cap moves, the argument becomes true in the form I stated it. Until then it
was true by coincidence.

And the way he found it is the part worth keeping: not by reasoning about the
round limit, but by trying to build a false positive and failing.

---

## 1. What the round limit is actually for

It detects **non-termination**: a graph that will not settle because each round
creates the work for the next.

Draining is the opposite of that. Each drained run strictly reduces work that
already existed, and the count cannot rise except by something in the step
re-triggering the head — which *is* the cascade case and should be caught. So
draining is not "the graph failing to settle", it is the graph settling, slowly.

The limit is counting the wrong events. That is the whole defect, and it is not
a tuning problem — raising the number moves the wall without fixing what the
wall is measuring.

## 2. Recommended: rounds that make definite progress do not count

Precisely, and this precision matters — a looser version breaks on pipelines:

> A round does not count against the limit if it consumed an activation that
> was **already pending when the step began**. Every other round counts.

Activations pending at the start of a step are a finite, known set. Working
through them is definite progress toward a fixed point. Activations *created*
during the step are not — creating and consuming work inside one settle is
exactly the shape the limit exists to catch.

| | rounds counted | outcome |
|---|---|---|
| drain 10,000 pre-existing runs | 0 | settles |
| `when` writing what its own trigger reads | all | fires, correctly |
| chain that re-arms itself from its own tail | all | fires, correctly |
| pipeline A → B → C created within one step | all | fires past depth 64, which is what the limit is for |

Cost is one bit per activation — *was I pending at step start* — and no extra
bookkeeping in the loop.

The three alternatives, for the record: **raising the limit** moves the wall;
**not counting draining rounds** is this proposal, stated loosely enough to
misfire on pipelines, which is why the "already pending" qualifier is doing
real work; **accepting a shallow cap** is defensible only with §4's diagnostic,
and it makes queue depth a language limit that nobody chose.

## 3. Keep one-run-per-round — it is load-bearing for a reason I had not stated

His third option — take several runs per round when they write disjoint cells —
is aimed at the right thing but the constraint is sharper than disjointness.

Activations are fungible, so `k` runs of a tail are *identical* computations.
Their writes are therefore identical values, and collapsing them is harmless —
**except when the tail reads a cell it writes**:

```ronin
shipped count = shipped count + 1
```

Three activations in one round each read the same front value and each write
`old + 1`. The count rises by one instead of three. One-per-round is what makes
that come out right, and it is the reason to keep it rather than the
write-collision argument I originally gave.

So the batching option is available but narrower than it looks: it is safe only
for a tail that reads nothing it writes, which is a static property and could be
checked. It is an optimisation, not a fix — a self-reading tail still drains one
per round and still meets whatever the limit is.

## 4. The floor, whatever else is chosen

He is right that this is the minimum, and I would put it first rather than last:

**The diagnostic must distinguish the two causes.** Today a drained queue is
reported as

> A when body is writing a var its own trigger reads, so every firing schedules
> the next.

which is a confident, specific, well-written description of a program the author
did not write. That is worse than a vague message — it sends them looking for a
bug that is not there.

Two findings, not one:

> «when ship» did not settle after 64 rounds: 64 runs were pending and each takes
> one round. This is a depth limit, not a cycle. *(and then whatever §2 decides)*

> «when ready» did not settle after 64 rounds: its body writes «ready», which its
> own trigger reads, so every firing schedules the next.

## 5. Sequencing

`K` after the cap, as he says. And the low-water-mark detector is unaffected by
any of this — it watches across steps, the round limit works within one, and the
two do not interact. That one can stay as built.

## 6. Calibration, since this is twice in two rounds

`ACCUMULATION-BOUND.md` §0 named the error class and then §1 committed it again
one level down: I asserted what a limit permits without reading the limit. The
pattern across this whole thread is consistent enough to state plainly —
**my reasoning about rules and semantics has held up; my claims about what a
specific mechanism does have not.** The useful division of labour is that I
should propose the rule and name what would falsify it, and he should run it,
because every time it has gone the other way the running has won.
