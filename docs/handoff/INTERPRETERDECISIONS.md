# Interpreter decisions

Answers to the three blockers. `reactive_core.py` is the executable version and
`reactive_scenarios.py` is the test list — 26 scenarios, all passing. Port the
scenarios first; they pin every decision below.

---

## 0. The rule everything else rests on

> **A `let` body is pure. It may not assign a var and may not touch a resource.**

Enforce it, don't assume it. Purity is what makes it safe to re-run a body an
arbitrary number of times, and *that* is what makes recompute, live editing,
record/replay, and parallel evaluation all work. Scenario 9 is the enforcement
test. Every other decision here is a consequence.

---

## 1. How `let` and `var` differ at evaluation

| | `var` | `let` |
|---|---|---|
| role | **source** | **derived node** |
| initialiser | evaluated **once**, at declaration | **not** evaluated at declaration |
| holds | a stored value | a cache plus a dirty flag |
| evaluating it | read the front buffer | if dirty, run body and cache; else return cache |
| assignment | writes the **back** buffer | illegal — only its body sets it |
| declaration order | matters | does not matter |

**Push dirty, pull values.** Writing a var *pushes* a dirty mark transitively
through dependents; reading a dirty node *pulls* a recompute. This is the hybrid
every modern signal library converged on, and it gives you three things free:
nothing recomputes that nobody reads, no topological sort is needed, and
glitch-freedom falls out (scenario 4 — the shared parent of a diamond recomputes
exactly once, in dependency order).

Two cheap wins worth building in from the start:

- **Equal-value writes wake nobody.** Assigning a var its current value marks
  nothing dirty (scenario 2). This kills a large fraction of real-world churn.
- **Stop marking at an already-dirty node.** Its dependents are already marked,
  so descending again is pure rework.

**Late-bound `let`** (from the guide: `let late-bound fastest horse => Horse;`)
is a node with no body yet. Reading it before initialisation yields an error
value, which then propagates like any other — consistent with §4, no special
case needed.

---

## 2. What a Call invokes

The gap is real: the resolver produces `Call(pattern, args)` and a `Pattern` is
only a *shape*. Fix it at declaration time.

**`Pattern` → `List<Declaration>`, not a single entry.** A list because
overloads share a shape and are separated later by type. Keep the phase order
we settled: enumerate readings → filter by type → rank by lookup count → tie is
an error. The resolver must be able to hand back several candidates; only the
type filter may cut them.

```
Declaration
    pattern   ('compute', 'total', 'for', HOLE)
    blocks    (('order',),)          -- parameter names, one tuple per hole
    body      callable
    pure      bool
```

**A hole is one parameter *block*, not one parameter.** The guide allows
`(x, y)` and allows brackets to be elided when fewer than two parameters are
bound. So the resolver hands over exactly one argument per hole, and the binder
destructures: a block of arity 1 binds the argument directly; a block of arity
*n* requires a bracketed group of *n* (scenario 10). This keeps the resolver
ignorant of arity, which is what you want — it already has enough to do.

**Two checks at the call site, before the body runs:**

1. If any argument is an error, return it. Bodies never run on error inputs.
2. If the call is inside a `let` and the declaration is not pure, that's an
   error naming the offending pattern. This is where §0 is enforced for calls.

**Purity is inferred, not declared:** a function is pure if it assigns no var it
doesn't own and touches no resource, transitively. Infer it, then freeze it at
the module boundary — the same treatment error-ness gets, for the same reason.

**Defer partial application.** `function add (other => Number) to 3 = 3 + ?;`
and `function getting stung = save the bees;` are a real feature and a
distraction right now. Full application first; leave `?` unimplemented and
loudly unsupported.

---

## 3. What reactivity means for the dependency graph

**Nodes:** one per `let`, one per `var`, one per resource.

**Edges are dynamic — recorded during evaluation, never read off the AST.**
This is the decision most likely to be got wrong, and it's not an optimisation:

```
let distance = if use metric then metres else feet;
```

This depends on `use metric` and *exactly one* of the other two. Read them off
the AST and changing `metres` wakes a node that no longer looks at it. Record
them during evaluation and it doesn't (scenario 3).

**Clear a node's dependency set before recomputing it.** Otherwise, when a
conditional switches branches, the stale edge keeps the node dirty forever. One
line, and the failure it prevents is miserable to debug.

**Cycles are an error, detected by re-entry.** Mark a node "evaluating" on
entry; if you re-enter it, that's a cycle — return an error value naming the
node (scenario 5). No static analysis required.

**Errors are values that flow through the graph, exactly like `#DIV/0!` in a
spreadsheet.** ~~A node whose dependency is an error becomes an error *without
running its body*.~~

**Corrected.** That is not achievable: an opaque callable cannot be aborted
without exceptions. The achievable guarantee is **adoption** — a node that reads
an error adopts it, and whatever its body returns is discarded. The body may
still execute, but because `let` bodies are **pure**, running one and throwing
the result away has no observable effect, which is what makes the weaker
guarantee equal to the stated one. Purity is load-bearing here rather than
incidental; without it, "the body may still execute" would be a hole.

Adoption and `lift` are both needed and neither covers the other. `lift` keeps
an error inert *inside* a body so its arithmetic never raises; adoption
guarantees the node inherits the error whatever the body chose to do with it —
including ignoring it entirely, where no operator is involved and `lift` never
sees anything. Fix the source and dirty marking recomputes everything
downstream, clearing it (scenario 6). Every builtin lifts to propagate errors;
`otherwise` is the single exception, and the only thing that inspects a
dependency's error state without inheriting it (scenario 7).

**One propagation step per batch.** All writes since the last step land
together, then dependents recompute. A reader can never see new `width` with old
`height` (scenario 8).

~~Double-buffer it: writes go to the back buffer, the step flips one index for
the whole graph, and reads stay plain loads. That measured identical to a plain
load and 3.6× faster than the queue alternative.~~

**Corrected.** The 3.6× came from `propagation.c`, which compared a FIFO ring
buffer against a double buffer — neither of which `reactive_core.py` implements.
It uses a pending map holding only the latest value per var, which is
latest-value semantics like the double buffer, so the figure was never evidence
for the prescribed change. The applicable measurement:

| writes | reads | map (µs) | dbuf (µs) | map/dbuf |
|---|---|---|---|---|
| 1 | 10 | 0.008 | 0.400 | 0.02× |
| 4 | 100 | 0.070 | 0.426 | 0.16× |
| 16 | 100 | 0.106 | 0.422 | 0.25× |
| 256 | 1000 | 1.581 | 1.185 | 1.33× |

Measured by `write_path.c`, and independently reproduced on other hardware: the
absolute microseconds differ, the ratios do not.

The map wins by 4–50× at realistic write counts, and the reason is structural: a
global index flip has to carry every unwritten var across the flip, so the step
is O(vars) where the map is O(writes). A frame writes a handful of sources and
reads many derived values — the top rows. Double buffering only pays once writes
approach the size of the graph. A generation-stamped buffer would avoid the
carry, but the thing tracking which vars changed is a dirty set, which is what
the pending map already is.

**Keep the map.** The genuine reason to double-buffer is readers running
concurrently with propagation, and parallel evaluation is deferred. Revisit when
it lands, at which point the question is a generation stamp rather than a global
flip.

---

## Deliberately deferred

Flag these as unimplemented rather than half-implemented:

- partial application (`?`) and function-by-equality
- `iterate` / `while` / `when` inside reactive contexts — imperative control
  flow interacting with the graph is the genuinely hairy part, and it's the one
  Budai explicitly wanted to defer until capabilities were pinned
- parallel evaluation — the graph makes it safe, but get it correct serially
  first
- resources — until there's I/O, the pure/effectful split can be enforced with
  nothing behind it

## Test order

Port scenarios 1, 2, 9 first (var/let split plus purity enforcement — everything
else depends on those), then 3 and 4 (dynamic edges, glitch freedom), then 10
(calls), then 5–8 (cycles, errors, `otherwise`, batching).
