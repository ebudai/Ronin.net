# Delta against `reactive_events.py` as shipped

> **Ledger** — `[R]` Delta against `reactive_events.py` as shipped
> supersedes: none
> superseded by: none

Nothing already implemented is wrong. This is one gap, one addition, and one
observation. `old_and_cycles.py` carries the code and the reasoning; this is
the part that says what to *do* with it.

---

## 1. GAP — cascades are tier 3 only

`reactive_events.py` ships the runtime cascade limit. That was the whole answer
when it was written; it's now the last of three.

```
1. STATIC    reject when-cycles at declaration, naming the whole ring
2. DECLARED  a when that genuinely needs feedback says so
3. RUNTIME   the cascade limit  <-- implemented
```

**Add tier 1.** The detector is `cycles()` in `old_and_cycles.py` — about
twenty lines, no body analysis. Build the graph "W1 precedes W2 when W1 writes
something W2 reads", DFS for cycles. On a sample set it finds self-loops, a
two-`when` ping-pong, and a three-hop ring through damage → death → respawn.

**Add tier 2** as whatever marks a `when` as intentionally cyclic. Without it,
tier 1 rejects `layout settle` — constraint relaxation legitimately writes the
sizes it reads until they stop moving, and banning that costs layout solving,
physics settling, and state machines that transition on their own state.

**Keep tier 3 exactly as it is.** Tier 1 can't distinguish a converging cycle
from a runaway one, and tier 2 is a promise a programmer can get wrong.

Error text for tier 1 should name the full ring, not just one participant —
the three-hop case is unreadable otherwise.

---

## 2. ADDITION — `old x` as an injected name

New since the file shipped, and it touches declaration rather than `when`.

- Declaring a `var` or `let` injects a second symbol into the same scope: the
  cell's name prefixed with `old`, typed `optional T`.
- **Not** for `when` triggers — `old <when name>` is meaningless. Injection is
  for value-holding declarations only.
- `old` becomes a reserved word: no pattern may use it as a segment. Without
  this, a single pattern like `recall (_) old (_)` puts `old` in the glue set
  and R5 rejects every injected name in scope.
- A user-declared name colliding with an injected one is a declaration error
  that names the injector.
- No `old old x`. Injection applies to declared cells, never to injected ones.
- Seed is `nothing`, never an error — an error seed **latches**: the cell
  errors, so next step its shadow is still an error, permanently. `optional`
  typing then makes a missing seed a compile error, and `otherwise` supplies
  it. No new checking.
- **Inject always, allocate lazily.** Whether `old x` is read is unknown until
  after resolution, but the name must be in scope *during* resolution. So
  declaration injects unconditionally; a post-resolution pass allocates a
  shadow only where a reference was found.
- The shadow copies `front` at the start of the step, before pending writes
  apply, so `old x` is the previous step's value for the whole step.

Worth documenting for users: a `let` that reads its own `old` advances **only
when observed**. `let tick = (old tick otherwise 0) + 1;` doesn't tick when
nothing watches it, because evaluation is demand-driven and the shadow copies
`front`, which only moves on recompute. Correct for a smoothing filter, wrong
for a clock. A clock is a `var` driven by the frame loop.

---

## 3. OBSERVATION — `when` and `old` are now the same mechanism

`reactive_events.py` keeps a per-`when` `previous` field for edge detection.
With `old` in the language, that's a second implementation of one concept:

```
when C { ... }          fires when   C and not old C
when y changes { ... }  fires when   y is not old y
```

Both are now expressible in the language itself, which means `when` could
desugar rather than be a third mechanism with its own state.

**Not a change request.** It's implemented and it works, and rewriting working
code to save a field is a bad trade right now. But the two must stay in step —
if the per-`when` `previous` and the `old` shadow ever disagree about when a
step boundary is, that's a bug with confusing symptoms. Worth a comment at both
sites pointing at each other, and worth a test asserting `when y changes` fires
exactly when `y is not old y` does.

If `when` ever gets rewritten, desugaring is the direction.
