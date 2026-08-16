# Reactive performance: granularity and fusion

> **Ledger** — `[R]` Reactive performance: granularity and fusion
> supersedes: not yet checked
> superseded by: not yet checked

**The worry:** a user writes a simulation the easy way, finds it slow, rewrites
it as one big imperative function, and concludes reactivity is a toy.

**The finding:** that worry is justified — by about 20× — but the cliff is
*granularity*, not reactivity. At the right granularity the tax is zero.

---

## The measurement

An economic model — a cobweb market with a global feedback term — implemented
four ways and run at four sizes. Microseconds per tick:

```
  goods    imper. scalar rx  array rx  fused rx   sca/imp arr/imp fus/imp
    100       0.2       2.5       0.2       0.2     13.4x   1.26x   0.85x
   1000       1.9      27.6       3.5       1.6     14.8x   1.85x   0.87x
  10000      18.5     366.1      38.5      17.7     19.8x   2.09x   0.96x
 100000     261.4    5180.7     737.8     248.7     19.8x   2.82x   0.95x
```

- **imperative** — flat arrays, tight loops. What a user writes after giving up.
- **scalar reactive** — one cell per good per quantity. 600,003 nodes at
  N=100,000. The naive translation.
- **array reactive** — one cell per *quantity*, each holding an array. Nine
  nodes regardless of N.
- **fused reactive** — same nine nodes, with the elementwise chain sharing one
  pass instead of materialising intermediates.

The scalar implementation is a real one, not a strawman: dirty flags,
demand-driven pull, dependency lists as index arrays rather than hash sets.

Honest reading of the last column: fused reactive **ties** a well-written
imperative version. It comes out marginally ahead only because the imperative
baseline also materialises two arrays it doesn't strictly need.

---

## Why a simulation is the worst case

Reactivity's advantage is recomputing only what changed. A UI click dirties
five nodes out of ten thousand, and the graph wins enormously.

A simulation tick dirties **everything**. The advantage evaporates and only the
bookkeeping remains — dirty flags set and cleared, dependency lists walked,
heap nodes chased — all to discover that yes, all 600,003 need recomputing.

That is the whole of the 20×. It is not a flaw in the propagation design; no
propagation design helps when the answer is always "recompute all of it."

---

## What follows for the language

### 1. Array-valued cells must be the natural spelling

If the obvious way to write a market produces one cell per good, users hit the
cliff and blame reactivity. This has to read as the default:

```
let demand = each of base minus elasticity times old price;
let total demand = sum of demand;
```

and per-agent cells have to be the thing you reach for deliberately, not the
thing you fall into. This is the same conclusion as the aliasing work, arriving
from the other side: **make the array primitive bulk, not indexed.**

### 2. Fuse elementwise chains

The largest single lever, and it costs no language surface — it's a graph-shape
decision. Nodes over the same index space, connected by elementwise operations
and reductions, can share one pass instead of writing an intermediate array per
node. That is the entire difference between the third and fourth columns.

Fusion ends where the shape changes: a scan, a sort, a gather, or a change of
index space closes the group. The compiler needs to know which node kinds are
fusable, which is a property of the operator, not of the program.

### 3. The compiler should say so

This is what stops users fleeing to imperative — not a faster runtime, but a
diagnostic at the moment the slow shape is written:

```
  «demand for (each good)» creates 100,000 cells of identical shape.
  An array-valued «let demand» would be one cell and about 20x faster.
```

A performance cliff users discover in a profiler becomes a performance cliff
that announces itself. The easy route stays easy *and* fast, and the slow
shape is the one that has to be chosen on purpose.

---

## A note the model made incidentally

The model is cyclic — price feeds demand feeds price — and `old price` is what
breaks the ring through the previous generation. Without it, a simulation loop
is not expressible at all under the ban on cycles.

`old` was introduced for moving averages. It turns out to be the mechanism that
makes every iterated simulation possible, which is a much larger job than the
one it was hired for.

---

## What this does not cover

- **Parallel evaluation.** Everything above is single-threaded. The graph makes
  parallelism safe, and the fused version is the one worth parallelising, since
  it is already bandwidth-bound rather than bookkeeping-bound.
- **Allocation.** The benchmark preallocates. A real runtime that allocates an
  array per node per tick would lose most of the fused win to the collector.
  Array cells want reused buffers, which the double-buffer shape already
  provides.
- **Sparse updates.** The opposite regime — a few nodes dirty out of many — is
  where reactivity is unbeatable and where no measurement was needed.
