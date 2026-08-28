# Runaway exact values — extend `Draining`, arm it statically, and never repair at runtime

**Three rulings.** §2: do not build a second watchdog — `Graph.Draining` is
already the right mechanism, and its growth predicate is better than the one I
first proposed. §3: a static pass selects *which* cells it watches, which this
hazard supports and the leak detector explicitly could not. §4: the repair is a
**proposed source edit**, never a runtime mutation.

One thing needs the designer: §6, whether this survives into a release build.

---

## §1 — the hazard, measured

Under silent promotion (no `large number`; `number` promotes past 64 bits),
denominators stay small for most programs and grow without bound for one shape:
**a value divided by something, whose result is divided again.** Measured, in
denominator digits — 64 bits holds 19:

```
                                     n=10   n=100   n=1000    promotes at
  sum of 2-decimal money amounts        2       1        1     never
  running average of readings           1       2        1     never
  running total split three ways        2       1        1     never
  harmonic sum 1/1 + 1/2 + ... + 1/n    4      40      433     n = 47
  feedback     x = x/3 + 1              5      48      478     n = 40
```

The second shape is what a reactive cell does for a living. Exponential
smoothing — `smoothed = old smoothed * 0.9 + reading * 0.1` — gains **one
denominator digit per update, forever**:

```
  after     40 updates      43 digits   <- past 64 bits
  after    120 updates     123 digits
  after   3600 updates    3603 digits   <- one minute at 60 updates/second
```

After an hour that cell's denominator is a quarter of a million digits and every
operation on it runs a GCD across it. The program does not crash; it grinds, and
memory grows without bound. In an always-running program that is the fatal shape
rather than a visible slowdown.

## §2 — extend `Draining`; do not build a second watchdog

`Graph.Draining()` already is this mechanism, built for pending `when` runs. Its
shape is the one to keep:

- **one watcher over the graph**, not one per scope — a single pass over an
  active set, with small per-subject state;
- it reports a **`Fault`**, not an exception and not a limit;
- `Settling` is documented as *PICKED, and allowed to be*, because it changes how
  quickly a leak is reported and not whether. **A denominator threshold is the
  same kind of number** — that retires the tuning question before it is asked.

**And its predicate is better than the one I proposed.** I said "monotone
increase over N samples." `Draining` watches the **low-water mark across
windows**, for the reason already written into it:

> *A queue comes back to nothing, sometimes slowly; an accumulation only ever
> rises. So this watches the low-water mark rather than the value, and reports
> when the quietest moment of one window is still busier than the quietest moment
> of the last.*

That transfers to denominators exactly, and mine would have been wrong: a
denominator that spikes and normalises back down is fine, and a monotone-increase
counter fires on it. What is fatal is a denominator whose **quietest** width keeps
rising window over window.

**Build it by extracting the window logic once, not by copying it.** The sampled
quantity differs (pending runs vs. denominator bit length); the window, the
low-water comparison, the settling count and the reset-at-rest are identical.
Two copies of that logic is how a stopping condition ends up stated in two places
that later disagree — the same reason finding 2 was landed with the refactor
rather than patched twice.

Per-subject state stays two numbers, as it is now: the window's low-water mark and
the previous window's. Sampling a numeric cell is `denominator.GetBitLength()`,
or a constant for an unpromoted value.

## §3 — a static pass selects what to watch, and this hazard supports one

`Draining`'s own comment is the thing to read carefully here, because it is right
about chains and does **not** generalise:

> *There is no static tier for this, unlike a cascade ring: a leak and a queue are
> the same shape at compile time, so nothing can be said before the program runs.*

True for pending runs. **Not true for denominators.** A cell whose new value
depends on its own previous value through a non-integer factor is a
*distinguishable shape* in a graph the runtime already builds:

```
  smoothed = old smoothed * 0.9 + reading * 0.1      self-edge, non-integer factor
  x        = old x / 2                               self-edge, non-integer factor
  midpoint = (old midpoint + target) / 2             self-edge, non-integer factor
```

The benign counterexamples are contrived (a factor that cancels itself). So this
hazard gets the static tier the leak detector was denied.

**Two things it buys, and the second matters more than the first.**

It warns earlier — but more importantly, **it keeps the runtime tier cheap.** The
watchdog samples only cells the static pass flagged, so the cost is proportional
to the number of *candidate* cells rather than to the size of the graph. Without
it, watching denominators means touching every numeric cell every window.

**The static pass must be silent.** It arms the watchdog; it does not report. A
compile-time warning on every smoother would be noise, and noise is how a real
warning gets ignored. Only the runtime tier — which knows the value actually grew
— says anything.

(A subcase *is* provable: `x = old x / 2` with no other input grows
unconditionally. Reporting that at compile time is a legitimate later refinement.
It is not worth building first.)

## §4 — the repair is a proposed source edit, never a runtime mutation

The instinct to have the watchdog *fix* the runaway is right about the goal and
wrong about the actor. A runtime repair means rounding a growing denominator to a
bounded one — and that is silent degradation of exactness, which this language has
now refused three times:

- `-0` as an error sentinel — refused;
- rounding at 64 bits — refused, in favour of promotion;
- an infinity — refused, because *"it would satisfy the hardware and then poison
  everything downstream silently."*

It is also **nondeterministic**: whether it fires depends on data and timing, so
the same program gives different answers on different runs and machines. Under
*debug is development*, where runs are compared against each other, that is a
property to keep.

So the repair is the one `Accumulating` already models — a fault that **names the
fix in words**, which the editor offers and the user accepts *into the source*.
That is the standing pattern: ambiguity-as-error offers bracketings, and the
source records which one was chosen. The tool proposes; the source decides; the
compiler never silently reinterprets.

And the always-running premise is what makes it cheap. The developer is sitting
there while the program runs on real data, so a one-click repair lands in seconds
rather than in a bug report weeks later. This is the case where *debug is
development* pays for itself rather than costing.

### What the fault should say

Model it on `Accumulating`: state the measurement, then name the repair.

> *«smoothed» holds an exact value whose denominator has grown to 3,603 digits
> and has not fallen in 12 windows. A cell that folds its own previous value
> through a fraction accumulates denominator without bound. If an approximation
> is intended here, «fast number» says so.*

The number and the direction are both load-bearing: the size alone does not
distinguish a large-but-stable value from a runaway, which is exactly why the
low-water predicate is the one being reported.

## §5 — what this does not cover

**Per-column promotion is a separate problem.** It addresses *storage* — keeping a
column unboxed while every element fits, promoting the column as a whole, and
demoting via an O(1) count of over-wide elements so a transient spike is not a
ratchet. The fatal case here is a **scalar** cell, so no column policy touches it.
Both are needed and neither substitutes for the other.

**The harmonic shape is not self-referential** and so is not caught by §3's
candidate rule — `total = total + 1/k` over many different `k` accumulates an
lcm without a self-edge through a fraction. It promotes at n=47. Whether the
candidate rule should also flag *accumulation of terms with varying divisors* is
worth a second look once the first pass exists; it is a wider net and likelier to
over-select, so it should not be in the first version.

## §6 — needs the designer: does this survive into a release build?

Hot reload was ruled a debug-only feature. The reflex is to treat this the same,
and I think the reflex is wrong here: the whole value of the runtime tier is
catching a data pattern the developer never had, which is *more* likely in
production, not less. The cost is a bit-length read per candidate cell per window,
on a path that has already allocated.

If it stays, it needs a destination — an end user must not see it, and it must
never become an error, because a program that is merely slow should not be turned
into a broken one.

## Summary

| | |
|---|---|
| **§2** | **extend `Graph.Draining`'s subject set. Do not build a second watchdog** — one watcher over the graph with small per-subject state is already the answer to *one or many* |
| its predicate | **low-water mark across windows**, not monotone increase. Mine would have fired on a spike that normalised back down. The existing comment gets this right and it transfers unchanged |
| its threshold | the same kind of number as `Settling` — *PICKED, and allowed to be*. It changes how quickly, not whether |
| how to build it | **extract the window logic once**; do not copy it. Two copies of a stopping condition later disagree |
| **§3** | a **static pass arms the watchdog**. `Draining`'s *"there is no static tier for this"* is true of chains and **not** of denominators — a self-edge through a non-integer factor is a distinguishable shape |
| why it matters most | it keeps the runtime tier **cheap** — sample candidates, not every numeric cell |
| and | **the static pass is silent.** It selects; it does not warn. A warning on every smoother is noise |
| **§4** | **the repair is a proposed source edit, never a runtime mutation.** Rounding a growing denominator is the silent degradation refused three times, and it is nondeterministic besides |
| the fault | model on `Accumulating` — state the measurement **and the direction**, then name the repair (`fast number`) |
| **§5** | per-column promotion is a **separate** problem: it fixes storage, and the fatal case is a scalar |
| also §5 | the harmonic shape has no self-edge and so escapes the candidate rule. Widen it only after the first pass exists |
| **§6 — needs you** | does the runtime tier survive into a **release build**? My lean is yes — its value is highest exactly where the developer is not — but it needs a destination, and it must never become an error |
