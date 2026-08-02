# The bound — and my §4 was the same mistake I wrote the note about

500 pulses, 500 pending, no runaway, no diagnostic. That refutes
`DIRECTION-PACKET.md` §4, and it does so by running the thing rather than
reasoning about it.

---

## 0. The error class, because it is a repeat

I claimed one-activation-per-round made the existing runaway detector
sufficient. I had not read the detector. `CODE-REVIEW.md` ends with a note I
wrote for exactly this:

> a checker's description of what it catches is a coverage claim needing its own
> adversarial test

I then made the claim about a checker I had not read, and it failed on the first
adversarial test anyone ran. The reasoning was even locally correct — one per
round *does* bound accumulation within a step — it just answered a question
nobody was asking, because the growth is across steps and the detector's window
is a step. **Right analysis, wrong scope, and reading the detector would have
shown it immediately.**

His framing is also sharper than mine: the counterexample family and the
accumulation gap are *the same failure*. Writing supersede as a chain does not
merely save ten times; it grows without bound while doing so. One mistake, two
symptoms.

---

## 1. Why a plain cap is the wrong instrument

The obvious answer is a limit — count exceeds N, report. It is wrong here,
because **the compiler cannot tell a leak from a busy queue.**

```ronin
when order placed { reserve; wait until payment cleared; ship }   10,000 pending is a Tuesday
when activity     { wait until 5 minutes; save }                  10 pending is a bug
```

Structurally identical. Any N either fires on the first program or misses the
second, and the second is the one that actually grows without limit.

## 2. What distinguishes them: draining, not depth

**A legitimate queue drains. A leak is monotonic.** Orders get paid, requests
get responses, so the pending count returns to zero — sometimes not for a while,
but it comes back. An accidental accumulation never comes back: the count only
ever rises.

So the detector should watch the **running minimum**, not the value:

> Track the low-water mark of a chain's pending count over a window of K steps.
> If the low-water mark has strictly increased across the whole window, the
> chain is accumulating. Report.

That fires on his 500-pulse program — the minimum climbs every step — and stays
silent on a deep queue that empties, however deep it gets. It is a leak
detector, which is the actual shape of the problem, rather than a size limit,
which is not.

A hard cap still belongs underneath it as a memory backstop, set high enough
that only genuine runaway reaches it. Two tiers, and the useful one is the top.

## 3. The report has to teach, because the fix is a rewrite

This is not a limit the author raises. It is a signal they picked the wrong
construct, so the message should say which one:

> «when activity» has 500 runs pending and the count has not fallen in 200
> steps. A chain gives each trigger its own run. If a new activity should
> supersede the pending one instead, use a deadline:
>
>     when activity       { save at = now + 5 minutes }
>     when now >= save at { save }

That is the guide entry delivered at the moment it is needed, which is what the
always-running environment is for — and it is the difference between a
diagnostic that stops the program and one that fixes the author.

## 4. Where this sits in the tiers

The cascade design already established the shape — static detection, then a
declared escape, then a runtime limit. Accumulation has the same three, and it
is worth noting that only the middle one is missing:

| tier | cascades | accumulation |
|---|---|---|
| static | SCC over the `when` graph | **nothing available** — a leak and a queue are structurally identical, so there is no compile-time test. This is why the guide is load-bearing |
| declared | `feedback` on the ring | nothing, and I do not think one is wanted: an author who declares "yes, unbounded" has almost certainly made the mistake |
| runtime | round limit within a step | the low-water-mark detector above, plus a hard cap |

The absence of a static tier is the reason he is right that the guide is
currently the only thing between an author and unbounded growth. That is a real
asymmetry with cascades and it should be written down rather than left implied.

## 5. Confidence, and what I would want run

The low-water-mark idea is design reasoning and has not been tested. Two things
I would want measured before it ships, and I would rather name them than repeat
§4:

- **False positives on slow drains.** A queue that empties every few hundred
  steps has a low-water mark that rises for a long stretch before falling. K
  needs to be chosen against a real workload, not picked.
- **Whether the low-water mark is cheap.** It is one integer per chain updated
  once per step, which sounds free, but I have not read the step loop and that
  is exactly the assumption that just cost us a round.
