# `when` — placement, lifetime, `stop`, and `wait until`

> **Ledger** — `[V]` `when` — placement, lifetime, `stop`, and `wait until`
> supersedes: none
> superseded by: WHENTYPESCOPE §1 (§1), WHENTYPESCOPE §3 (§4), DIRECTIONPACKET §2 (§4), CHAINACTIVATIONS §3 (§5.3)

Handoff for the programmer. Design decisions, agreed. Written against the
runtime as of `7cebf9a` on `resolver-and-symbol-separation`, where `Graph` holds
`nodes`, `whens`, `constants`, `shadows` as flat `Dictionary<string, …>` and
there is **no instance concept yet**. Several items below depend on that, so
where they say "decide before instances exist", that is not rhetorical.

Nothing here is urgent relative to the audit. It is written down now because
two of the decisions are cheap today and structural later.

---

## 1. Placement: `when` is module-scoped or type-scoped, never local

A `when` may be declared at **module scope** or at **type declaration scope**.
It may not be declared inside a function body, a block, or another `when`.

The reason is that a local `when` has no coherent lifetime. A propagation step
happens between statements, not during one, so a `when` declared inside a
function either

- exits its scope before any step runs — in which case it can never fire and
  the declaration is dead code; or
- outlives its scope — in which case it is a leak, holding references to locals
  that are gone.

There is no third option, so the restriction costs nothing and removes a whole
category of confusing behaviour.

**Diagnostic.** New `FindingKind`, primary span on the `when` keyword, related
span on the enclosing function or block declaration.

> «when» may only be declared at module scope or inside a type. A «when»
> declared inside «update path» would go out of scope before it could ever run.

---

## 2. Lifetime: a `when` stops when its scope exits

Module-scope `when`s live for the life of the module. Type-scope `when`s live
for the life of the instance. That is the whole rule, and rule 1 is what makes
it a whole rule.

---

## 3. Instance binding — the decision that has to be made before instances exist

Type-scope `when`s bound to the instance, with access to instance members, is
the right semantics. It is also the point where the runtime acquires a concept
it currently does not have, and the naive shape is the performance cliff we
already measured.

**The naive shape:** each instance gets its own nodes, so `N` enemies with
`when health <= 0 { die }` produce `N` predicate nodes, `N` health cells, `N`
sets of edges. In `econ_sim.c` that per-instance-scalar-node shape ran **~20×**
slower than the grouped equivalent, and the cost is cache and edge-chasing, not
arithmetic — it does not go away with tuning.

**The shape to build instead:** one cell per **member**, holding `N` values.
`health` is a single node whose value is an array of `N` numbers. `when health
<= 0` is then one node evaluating a vectorised predicate over the array, and
firing the body once per instance whose entry flipped false→true.

Consequences to be aware of, all of them acceptable:

- Instance identity is an index into the member arrays, not a pointer.
- Adding and removing instances is an array operation, and removal wants a
  free-list or a swap-with-last plus a stable-handle table, not a shift.
- Dependency edges are per **member**, not per instance, so the graph stays the
  size of the *source text* rather than the size of the *world*. This is the
  actual win, and it is much larger than the vectorisation.
- Cutoff (see below) becomes array-valued and therefore O(N) per cell — use a
  dirty-range or a small digest, not a full compare.

The reason to decide it now: the grouped layout is not a representation you can
retrofit under an already-written per-instance runtime. Everything that touches
`nodes` by string key has to know which of the two worlds it is in.

---

## 4. `stop`

Agreed, and it is small.

- **`stop` is legal only in a `when` body.** It is an effect, so the existing
  rule that `let` bodies may not perform effects already excludes it from
  `let`; make the diagnostic specific anyway rather than letting it fall out of
  the generic purity violation.
- **It takes effect at the end of the round**, like writes. A `when` that stops
  itself finishes its current body.
- **It must actually remove the node**, not set a disabled flag. A disabled node
  still costs an edge walk and still participates in cascade counting, and
  "stopped" that isn't gone is exactly the leak we removed in rule 1.
- **It can only shrink the graph, therefore it cannot make a legal program
  illegal.** SCC analysis over the never-stops graph stays sound: removing
  nodes can only remove cycles. So `stop` needs no interaction with the cascade
  checker at all, which is worth stating in a comment next to `Cascades` so
  nobody later "improves" it into a dynamic analysis.

---

## 5. `wait until` — compiled away, not run

`wait until` looks like a coroutine feature: it implies a continuation,
per-activation state, and a re-entrancy policy. We are not building coroutines
for it. A suspended continuation is live state produced by *old code*, which is
the live-edit migration problem at its very worst — a hot reload lands mid-body
in a function whose body has changed.

Instead the compiler splits the `when` into a chain.

### 5.1 The desugaring

Source:

```
when A {
    x
    wait until B
    y
}
```

Becomes:

```
when A {
    clear all flags in this chain
    x
    set «flag 1»
}
when B and «flag 1» {
    clear «flag 1»
    y
}
```

`n` waits produce `n + 1` `when`s and `n` flags. Segment `k` runs under
`when B_k and «flag k»`, clears `«flag k»`, and sets `«flag k+1»` if there is
another segment.

### 5.2 The flags are not variables

This is the part that will bite if it is missed. A chain flag must **not** be
an ordinary `var` node, for two independent reasons:

- **Single-writer.** `«flag 1»` is written by the first `when` (set) and by the
  second (clear). Two writers on one cell, which the writer analysis correctly
  rejects.
- **Cascade analysis.** The second `when` reads `«flag 1»` and writes
  `«flag 1»`, which is a self-loop in the `when` dependency graph, so the SCC
  checker reports every split `when` as undeclared feedback.

So a chain flag is **runtime state on the chain**, wired by the compiler,
invisible to the dependency graph and to `Cascades`. The compiler emits the
edge `first when → second when` directly, which is the true dependency and the
one you want the ring checker to see.

Naming follows the `old x` precedent: these are injected symbols, they carry
the span of the declaration that caused them, and no diagnostic ever asks the
author to rename one.

### 5.3 Re-entrancy: restart is the default

The split forces an answer to *what happens when `A` fires again while the
chain is mid-flight*, and one flag admits exactly two:

**Restart (default).** The first `when` clears *every* flag in the chain before
setting `«flag 1»`. Any in-flight activation is abandoned wherever it was, and
the chain begins again. This must clear **all** downstream flags, not just set
flag 1 — otherwise a re-fire while the chain sits at segment 3 leaves two live
positions and the tail runs twice.

**Ignore.** The author writes it, in one clause:

```
when A and not «A in flight» { … }
```

where `«A in flight»` is a compiler-provided derived value, true when any flag
in the chain is set. Debounce, one-shot, and "don't retrigger the animation"
are all this.

Anything richer — queueing activations, running several concurrently — needs
state the author writes explicitly. That is the correct place for it to live.

The point worth keeping in view: with a real coroutine, re-entrancy policy is
invisible runtime behaviour learned about from a bug report. Here it is two
`when`s and a bit, both readable. If the Workbench can show the desugared pair
on request, the semantics are never hidden — which is the whole reason this
approach is better and not merely simpler.

### 5.4 What gets rejected

Splitting requires the waits to be **statically sequential**. Rule: a
`wait until` is legal only as a statement directly in the `when` body, not
nested inside any control flow.

Rejected, each with its own diagnostic:

| Shape | Why | Message should say |
|---|---|---|
| `wait until` inside a loop | needs per-iteration state, unbounded | write the state explicitly, or restructure as a `when` on the loop condition |
| `wait until` inside an `if` | the continuation differs per path | hoist the wait out of the branch |
| `wait until` inside a called function | invisible to the splitter | `wait until` must appear directly in the `when` body |
| `wait until` in a `let` | effect in a pure body | (existing purity diagnostic, but name `wait until`) |

The function case is the one to write the best message for, because factoring a
`when` body into a helper is the natural thing to do and this is the one place
it stops working.

### 5.5 Time-based waits

`wait until 3 seconds` needs a deadline, not a boolean: the first segment
stores `now + 3 seconds`, and the guard is `now >= «deadline 1»`.

That makes `now` a source that changes every round, and every pending timer a
`when` reading it. For a handful of timers this is fine. For thousands it is
the same granularity cliff as §3 — every timer wakes every tick to compare.

If timers become common, the fix is the standard one: a single earliest-
deadline node, so only one comparison happens per tick and only the chains that
actually expire wake up. Not needed now; worth not designing it out.

### 5.6 `stop` on a chain removes the whole chain

The author wrote one `when`. `stop` anywhere in the chain must remove **all**
the `when`s the chain compiled to and clear all its flags. Same for scope exit.

If `stop` only removes the half it appears in, an armed first half leaves an
orphaned second half that fires whenever its condition eventually goes true —
possibly much later, with the rest of the chain gone. That is a genuinely
horrible bug to diagnose, and it is prevented by recording chain membership at
split time.

---

## 6. Test list

Placement and lifetime:

1. `when` inside a function → the new diagnostic, primary span on `when`.
2. `when` at module scope survives across steps.
3. Type-scope `when` stops when the instance goes away.

`stop`:

4. `stop` in a `let` → diagnostic naming `stop`.
5. `stop` takes effect at end of round: a `when` that stops itself completes
   its body, including writes made after the `stop`.
6. After `stop`, the node is absent from `whens`, not present-and-flagged.
7. A program whose cascade analysis passes still passes with `stop` present
   (the shrink-only property) — assert `Cascades` output is unchanged.

`wait until`:

8. Single wait: `A`, then `B`, runs `x` then `y`, once.
9. `B` true before `A` fires → `y` does not run early.
10. `B` becomes true and then false again before the step → normal edge rules,
    no special case.
11. Two sequential waits → three `when`s, correct order, each flag cleared.
12. Re-fire while in flight, default → restart; the tail runs exactly once.
13. Re-fire while in flight at segment 3 of 3 → **all** flags cleared;
    assert the tail does not run twice. This is the one that catches the
    partial-clear bug.
14. `when A and not «A in flight»` → the second fire is ignored, tail runs once.
15. `stop` in the second half → the first half is gone too; make `B` true
    afterwards and assert nothing runs.
16. Scope exit mid-flight → same assertion as 15.
17. Chain flags do not appear in the dependency graph: `Cascades` reports no
    ring for a split `when` (the self-loop regression).
18. Single-writer analysis does not fire on a chain flag.
19. Each rejection shape in §5.4 produces its own `FindingKind`.

---

## 7. Related items still open, for context

Not part of this, but adjacent and worth not contradicting:

- **Cutoff on `Recompute`** is still absent in `Graph.cs`. `Propagate` has the
  equal-write check at line 321; `Recompute` sets `Value` and clears `Dirty`
  with no comparison. This matters more for `when` than for `let`, because
  `old` shadows advance every step and an uncut shadow wakes its dependents
  every tick forever — the graph never goes quiet even when nothing happens.
- **Multi-span findings.** Several diagnostics above name two things (the
  `when` and its enclosing scope; the `wait until` and the loop it sits in), so
  they want `Related` populated for real rather than only in hand-built test
  data.
