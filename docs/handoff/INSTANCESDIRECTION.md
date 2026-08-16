# Instances — direction, and a correction to something I nearly shipped

> **Ledger** — `[V]` Instances — direction, and a correction to something I nearly shipped
> supersedes: DOTNETSCHEDULER §2–§3
> superseded by: none

Two parts. The correction first, because it changes an instruction in a document
that has not gone out yet.

---

# 1. Correction: the SIMD claim in `DOTNET-SCHEDULER.md` is wrong

`DOTNETSCHEDULER.md` §2 says vectorisation gives 2.8× and should come before
threading. **That number is an artifact of an unfair baseline.** In
`Program.cs` the sequential column was timed as a lambda closing over its
arrays, while the SIMD column called a static method — so the baseline paid
display-class indirection the SIMD path did not.

Measured fairly, both as static methods, on the same kernel:

```
n           sequential   Vector<T> ctor   span-cast (no bounds checks)
4096           8014 ns    12484 (0.64x)    7484 (1.07x)
65536         69214 ns    55185 (1.25x)   59465 (1.16x)
1048576     1429418 ns  1424760 (1.00x)  1352894 (1.06x)
```

**Vectorisation buys essentially nothing here**, and the naive
`new Vector<double>(array, i)` form is *slower than scalar* at small sizes,
because it pays a bounds check per load and per store — the same count the
scalar loop pays per element.

The reason is not .NET. The kernel reads three cells, does three multiplies and
two adds, and writes one — 32 bytes of memory traffic for five flops. It is
bandwidth-bound, so widening the arithmetic changes nothing. **Reactive node
bodies are mostly this shape**, which makes vectorisation a much weaker lever
for Ronin than I claimed.

So `DOTNETSCHEDULER.md` §3's "vectorise first, thread second" is **withdrawn**.
The threading numbers in it stand — those were measured with closures on both
sides, so that comparison was fair — and `Parallel.For` at 8.8 µs versus a spin
pool at 635 ns stands too. Only the SIMD ordering was wrong.

I caught it by adding a sequential anchor to a benchmark that did not have one.
Without the anchor there was no way to tell a result from a misbehaving loop,
which is the same lesson as last round, arriving from the other direction: *I*
was the one whose probe did not do what I meant.

---

# 2. The measurement that matters for instances

The question `INSTANCE-BINDING.md` never asked: **what happens when only a few
instances change?**

One node per member means one dirty node means recompute over all `N`. That is
`O(N)` for one instance's change, where per-instance nodes were `O(1)`. Dense
updates — a simulation ticking everything — are the good case. Sparse updates —
three entities changed, a form where one control moved — are the bad one, and
they are what an application spends its life doing.

```
                       scattered dirty list vs full pass
dirty      n=4096       n=65536      n=1048576
0.1%       100x          111x           63x     ← scattered
1.0%        24x           12x           11x     ← scattered
5.0%       6.1x          1.6x          1.5x     ← scattered
10%        2.8x          1.1x          1.1x     ← scattered
25%        1.1x          1.2x         0.96x     ← crossover
50%       0.98x          1.0x         0.91x     ← full pass
100%      0.74x         0.60x         0.83x     ← full pass
```

**A dirty index set per member array is a requirement, not an optimisation.**
Without one, the sparse case — the common case — is 60× to 110× worse than it
needs to be, and that would eat the entire 7.7× the layout decision buys.

The crossover is around a third dirty, consistently across three orders of
magnitude of `N`. So:

> Each member array carries a dirty index set. Recompute scattered while the
> set is below about a third of the array; above that, discard the set and do a
> full contiguous pass. The threshold is a measured constant, not a derived one,
> and it belongs next to the low-water-mark window as a tunable.

Cutoff falls out of the same structure: comparing only the dirty indices makes
it `O(k)` rather than the `O(N)` that `WHENANDWAIT.md` §7 worried about.

---

# 3. What still needs deciding before instances are built

**Identity: stable handle outside, index inside.** Removal is swap-with-last, so
raw indices are invalidated by an unrelated removal — a stored reference would
silently point at a different instance, which is the exact failure class this
project keeps refusing. A handle is a (slot, generation) pair; the generation
catches a stale handle and turns it into an `Error` rather than a wrong answer.
Indices exist only inside the runtime, where the arrays are.

**Per-instance activation counts.** A type-scope `when` with a `wait until` needs
its counts per instance — an array, not a scalar. The low-water-mark detector
then sums the array once per step, which is the same cost as today. The
`limit + 2k` accounting is unchanged: `k` is the total inherited across
instances.

**Creation and destruction syntax.** This is a language question rather than a
runtime one, and nothing has been designed. It is the one item here I would not
guess at.

**What does not change:** cascades and rounds are per-node, not per-instance, so
none of the round accounting settled over the last few rounds is affected.

---

# 4. One thing to check, given `IFASEXPRESSION.md` has been implemented

That document predates `BRACEDECISION.md`. It makes a block an expression, and
`{ }` already opens a block, a list and a lookup — so if it went in without the
brace question being settled, `{ 1 }` is now ambiguous between a one-element
list and a block yielding `1`, in the same position, with different types. The
auditor's `{ 1, 2 } [0] + 3` is the case that shows it.

Worth asking the programmer whether that came up during implementation. If it
did not, it is live rather than absent.
