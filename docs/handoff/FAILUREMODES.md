# Failure modes from the literature, mapped onto Ronin

> **Ledger** — `[R]` Failure modes from the literature, mapped onto Ronin
> supersedes: not yet checked
> superseded by: not yet checked

Seven. Two are holes we have not predicted and one of those is structural.
Three are predicted but under-mitigated. Two we are immune to, and knowing
*why* matters because the immunity is a constraint we must not casually drop.

Ordered by how expensive they are to fix later.

---

## 1. STRUCTURAL HOLE — modules compose into conflicts neither module has

**Status: unpredicted, and our stated rule does not cover it.**

Schwerdfeger & Van Wyk's [Verifiable Composition of Deterministic
Grammars](https://dl.acm.org/doi/10.1145/1543135.1542499) exists because
independently-developed language extensions, each deterministic alone, can be
non-deterministic composed. Ronin has this in a sharper form, because every
declaration is a grammar production and R5/R6 are properties of the **merged**
table:

```
module A exports    pattern  send (_) to (_)      ← compiles, legal
module B exports    name     hello to alice       ← compiles, legal
importer            import A; import B;           ← R5 VIOLATION
```

Neither module is at fault, and the importer can edit neither. `SCOPING.md`
says "reject the inner declaration" — but with two imports there is no inner
one. The rule has no answer here.

### Mitigation, and it is a decision that should be made now

**A module's own statements resolve against the scope it was compiled in, not
against any importer's merged table.**

That is the separate-compilation guarantee, and it is what makes the failure
survivable: importing two modules can never change the meaning of either
module's code. A's `send hello to alice` means what A meant, permanently.

The conflict then confines itself to the importer's **own new statements**,
which is the one place a human can fix it. Two tools for that, and the guide
already has the shape of the first:

```
import matrix math = standard math algebra;      ← aliasing already exists
```

Extend it to selective aliasing, and make a conflicting name reachable only
qualified — so it never enters the flat merged table unqualified in the first
place.

**Why this is urgent rather than merely important:** without the
compiled-scope rule, adding an import can silently re-resolve statements in a
module you did not touch. That is the R5 hazard again, one level up, and it
does not have a local fix once the language ships.

---

## 2. REAL HOLE — no cutoff on recompute

**Status: unpredicted. One comparison to fix. Measured cost is 97%.**

Equal-value suppression exists on `write` but not on `recompute`. A `let` that
recomputes to an unchanged value still lets the dirty wave run past it:

```
59 one-pixel mouse moves; «hovered row» changes twice
  downstream recomputes without cutoff:  177
  downstream recomputes with cutoff:       6
  wasted:  171 recomputes, 97% of the total
```

This is standard in the incremental literature — Jane Street's
[Incremental](https://www.janestreet.com/tech-talks/seven-implementations-of-incremental/)
calls it *cutoff*, and it is one of the seven rewrites' hard-won lessons.

### Mitigation

In `recompute`: if the new value equals the old, leave the dependents' dirty
flags **set to clean** rather than propagating. One comparison.

**It matters more in Ronin than elsewhere**, because `old` shadows advance
every step. Without cutoff, a shadow copying an unchanged value wakes its
dependents every tick forever — a graph that never goes quiet even when
nothing is happening.

Caveat worth stating: equality has to be the language's equality, and for
array-valued cells that is O(n). Cutoff on a large array can cost more than
the recompute it saves. Cheap answer: cutoff scalars unconditionally, and for
arrays compare a cheap digest or skip the check.

---

## 3. UNDER-MITIGATED — node lifetime under live editing

**Status: predicted in outline, no mechanism.**

The FRP space-leak literature is largely about *higher-order* signals retaining
history — see [FRP without spacetime
leaks](https://www.cl.cam.ac.uk/~nk480/simple-frp.pdf). Ronin's exposure is
different and comes from live editing: delete a `let` from source and its node,
its edges, and its `old` shadow must all disappear. Miss any, and an editing
session accumulates dead nodes that still get dirtied.

### Mitigation

Node lifetime tied to declaration lifetime, with removal scrubbing the name
from every `dependents` set — and a periodic reachability sweep in the live
environment as a backstop, because edit-driven removal *will* have bugs and the
symptom otherwise is a session that gets slower the longer you work.

---

## 4. UNDER-MITIGATED — bidirectional inference makes overload resolution a solver

**Status: not predicted. Cheap to prevent now, very expensive later.**

The phase order — enumerate, type-filter, rank, tie — assumes types are known
*before* filtering. If argument types can depend on which overload is chosen,
the filter becomes a constraint solver. That is how Swift's type checker earned
its reputation, and how C++ overload resolution became unteachable.

### Mitigation

**Rule it out now: types flow outward-in only.** An argument's type is
determined before the call it appears in is selected. No return-type-directed
overload selection, ever. That keeps the filter a filter.

It costs one convenience — `let x => Distance = parse "3km"` picking an
overload by expected type — which is worth losing.

---

## 5. UNDER-MITIGATED — exactness contagion at the wrong moment

**Status: Scheme found these forty years ago.**

We decided roots and transcendentals return `fast number`. Taken literally,
`square root of 4` is inexact — the classic Scheme papercut, where a perfect
square silently leaves the exact world.

### Mitigation

Copy Scheme: **return exact when the result is exactly representable.**
`square root of 4` → exact 2. `square root of 2` → `fast number`. Same for
integer powers and exact logs. The boundary is "can this result be represented
exactly", not "which function was called".

That makes the rule slightly harder to state and much less surprising, which is
the right trade in a language whose premise is that the obvious reading is the
true one.

---

## 6. IMMUNE — and the immunity is a constraint, not luck

**Higher-order reactive values.** The FRP space-leak literature needs signals
of signals; a leak needs a cell whose *value* is another cell, retaining
history that nothing can reach but nothing can free.

Ronin has no such thing. A `let` holds a value, never a cell.

**This is worth writing into the spec as a deliberate restriction rather than
leaving it as an accident.** The moment someone adds "a cell can hold a
reference to another cell", forty years of leak literature becomes relevant and
none of the mitigations are cheap. Say no now, while saying no is free.

---

## 7. IMMUNE — reactive debugging, and we can go further

Stack traces are meaningless in reactive systems: the caller is always the
propagation engine. It is the most-cited practical complaint in the
[reactive programming survey](https://dl.acm.org/doi/10.1145/2501654.2501666).

Ronin is not merely immune — it can answer the question the trace was for.
**The dependency graph is the explanation.** "Why did this value change" is a
graph query: which dependency changed, which of *its* dependencies changed,
back to a source or a `when`.

That should be a first-class feature of the always-running environment, not a
debugger afterthought. Almost no reactive system can answer it, because almost
none keep the graph reified at runtime. Ronin does, for other reasons.

---

## Order I would take them

1. **Compiled-scope resolution** (§1) — it is a language semantics decision and
   it stops being fixable once modules exist in the wild.
2. **Cutoff** (§2) — one comparison, 97% of the wasted work in the measured case.
3. **Outward-in typing only** (§4) — a rule to write down, not code to write,
   and it prevents a class of problem rather than fixing one.
4. **No higher-order cells** (§6) — same: a sentence in the spec now, versus a
   research programme later. NOTE: this is no longer purely hypothetical. `() =>
   …` is well formed (see `DELEGATES.md`), and a zero-argument delegate whose
   read invokes it is a deferred computation held in a cell — so the language has
   higher-order cells whether or not the prohibition is written down. Deciding
   it late now means deciding it against existing programs.
5. Node lifetime (§3) and exact roots (§5) when the live environment and the
   numeric tower respectively get built.
