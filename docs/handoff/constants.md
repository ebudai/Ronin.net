> **Ledger** — `[V]` Constants and static-init order — order constants together with the var initialisers they read; cycles banned.
> supersedes: not yet checked
> superseded by: not yet checked

me: yes for constants, this is perfect.  regarding static-init-order trap: we should enforce dependency order, cycles banned.

programmer: Agreed — with one extension, because dependency order among constants doesn't cover the case that actually causes the trap.
Ran a command

Agreed. One extension, because the case that actually causes the trap isn't constant-to-constant.

constant initial health = health; reads a var. Ordering constants among themselves never places it — health isn't a constant, so it isn't in that graph at all. Put var initialisers in the same graph and it resolves:

evaluation order:
  1. detail level       (constant)
  2. pi                 (constant)
  3. circle segments    (constant)
  4. difficulty         (constant)
  5. max health         (constant)
  6. health             (var)
  7. initial health     (constant)

That makes the snapshot case well-defined rather than order-dependent, so it needs no warning at all — better than what I proposed last turn. And cycles across the mixed set are caught by the same DFS already written for when rings:

difficulty -> initial health -> health -> max health -> difficulty

Four hops, three declaration kinds, one detector pointed at a different node set.

Two consequences worth settling while this is open:

A constant whose initialiser errors should fail startup, not hold an error value. Same reasoning as the old seed: an error in a constant can never clear, so it latches permanently and every reader inherits it forever. constant config = load settings from file; failing should stop the program with a diagnostic naming the constant, not quietly poison half the graph. That's the second place the latch argument has decided something — worth noting the pattern, since it'll come up a third time.

A constant reading a let is well-defined but worth documenting. It forces the let's computation at init and freezes that value; the let later recomputes on its own dependencies while the constant keeps the snapshot. Correct, and probably what the name implies, but surprising enough to belong in the guide rather than in a warning.
