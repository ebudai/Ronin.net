# Take (A) — and the thing that stops the treadmill is in neither option

> **Ledger** — `[V]` verdict. Rules **(A)**: one grammar-driven admissibility walk,
> one source for `ActionInValue`. Adds the requirement that decides whether this is
> the last audit on the rule — **the position function must be total over grammar
> node kinds, compiler-checked**. Refuses (C). Defers (B)'s stored role with a
> trigger.
> answers: `VALUEPOSITIONS`
> supersedes: none
> superseded by: `VALUEPOSITIONSBUILDRULING`

**Take (A).** But the reason (A) is right is not the reason you gave for it, and
**neither (A) nor (B) stops the treadmill on its own.** §2 is the part that does.

You were right to stop at three. *"That pattern says the approach is wrong, not the
patch"* is the same call you made before the fifth `Unanswered` heuristic, and it
was right then too.

---

## §1 — (A), because two mechanisms computing one fact diverge

Your own measurement is the argument. `send (act 1)` draws **two findings at two
spans**, because `Arguments`/`Disagreeing` and `Inadmissible` both compute *is this
an action in a value position* and disagree about where to say so.

That is the sixth instance of one failure in this codebase:

```
  two window predicates          Draining vs. a copied one
  two Tarjans                    Cascades vs. a re-derived fixpoint
  two invalidation designs       the graph vs. a new cache scheme
  two literal classifiers        Lexicon.Literal vs. Evaluator's hand-roll
  «char.IsDigit» twice           any Nd to the lexer, 0-9 to the evaluator
  two admissibility mechanisms   Disagreeing vs. Inadmissible          <- here
```

**One fact, one mechanism.** So `Disagreeing` stops emitting `ActionInValue`
entirely, and the walk is the only source. The double-report then cannot occur —
not because it is deduplicated, but because there is nothing to deduplicate.

Note what that does to (B): as you describe it, `Disagreeing` keeps emitting, so
(B) **keeps the double-report**. If (B) also removes it, then (B) *is* (A) with the
role stored rather than transient — which is §5.

## §2 — the requirement that actually ends this: **totality**

Here is what I think both options miss. There are **two** defects, not one:

| defect | needs | fixed by |
|---|---|---|
| a reading's **root** has no role — `var r = act 1` and `act 1;` are one reading | grammar context | (A) or (B) equally |
| the **child** enumeration is partial — `Positions` returns none by default | **exhaustiveness** | neither, as written |

The second is the one that has failed three audits. REAUDIT77 found operands;
78 found peers and `otherwise`; 79 found calls, round groups, omitted returns.
Every round added a case to a switch that silently returns *none* for everything
it has not been taught.

> **The position function must be total over grammar node kinds, with no default
> arm.** A construct the checker has never seen must **fail the build**, not
> silently admit an action.

That converts *"someone must remember to add the case"* into *"the compiler will
not let you forget."* It is the same move as the `Variable` factory — make the
invalid state unconstructible — and the same property the lexer already holds:
every character in exactly one token, no silent gap.

Without it, (A) is the fourth patch. With it, (A) is the last one.

## §3 — the proof that hand-enumeration cannot be the mechanism

This is not a theoretical worry. **Your own list of value positions is
incomplete**, and every omission is a construct that is a verdict in force:

```
  a «when» condition                     WHENANDWAIT       [V]
  a «wait until» argument                WHENANDWAIT       [V]
  an «if» condition, and its arms        IFASEXPRESSION    [V]
  a «for each» iterable                  LOOPSYNTAX        [V]
  a «match» inline arm                   MATCHNAMED        [V]
```

`when act 1 { … }`, `for each x in act 1 { … }`, `if act 1 { … }` — each is an
action in a value position, and none is in the set.

That is not a criticism; it is the evidence. **The person who has spent three
audits enumerating value positions still produced an incomplete enumeration.** No
amount of care fixes that, which is exactly why §2's requirement is the ruling and
not a nicety.

Write the set down in the ruling *and* let the compiler enforce it. The written
list is for a reader; the exhaustive switch is what keeps it true.

## §4 — (C) refused, and it is a familiar shape

Containment-dedup is *"a new rule with its own edges"* — your words, and they are
the diagnosis. A dedup rule that silently picks a winner between two nested spans
makes *which finding survives* load-bearing, and nothing states which should.

More simply: it is the fifth heuristic. You declined to ship one of those in the
`Unanswered` treadmill for the right reason. The same reason applies.

## §5 — (B)'s stored role: not now, and here is when

Do not add the field in this change — under (A) the role is computed and consumed
in one descent, so storing it is a second representation of a fact with one
consumer.

But your instinct that *"readings carry a role they arguably should have anyway"*
is not wrong, and there is a real trigger coming:

```
  approximation                     successor                      trigger
  the grammar role is transient,    a stored position role         the LSP completion
  computed during the walk          with a second consumer         slice
```

Two conditions if it ever lands. **It must be computed by the same walk** — a
second walk computing roles is §1's defect again. And **it should be keyed by
position, not by reading**: completion asks *what is expected at this cursor*,
which is finer than a reading, and a role field on a `Reading` only ever carries
the root's.

## §6 — and the legal case needs no carve-out

Worth noticing as confirmation you have the right model: under (A), *"a standalone
call to an action is the normal way to perform it"* requires **no special case at
all.** A statement is not a value position, so its root is never checked. The audit
warns against a blanket root rule; (A) never has one to exempt.

A rule whose legal cases need no exceptions is usually the right rule.

## Summary

| | |
|---|---|
| the ruling | **(A)** — one grammar-driven walk, and **`Disagreeing` stops emitting `ActionInValue`** |
| why | your measured double-report is **two mechanisms computing one fact**, the **sixth** instance of that failure here. One fact, one mechanism — then there is nothing to dedup |
| what (B) does | as described it **keeps the double-report**; if it also removes `Disagreeing`'s, it *is* (A) with the role stored |
| **§2 — the real ruling** | there are **two** defects. Root ambiguity needs grammar context (either option). **The partial child enumeration needs totality**, which neither option supplies |
| the requirement | **the position function is total over grammar node kinds, no default arm.** An unknown construct **fails the build** rather than silently admitting an action |
| without it | (A) is the fourth patch. **With it, (A) is the last one** |
| **§3 — the evidence** | **your own set is missing five** — a `when` condition, a `wait until` argument, an `if` condition and arms, a `for each` iterable, a `match` arm. All five are `[V]` constructs |
| what that proves | not carelessness — that **hand-enumeration cannot be the mechanism.** Write the set for readers; let the compiler keep it true |
| **(C)** | **refused.** Containment-dedup makes *which finding survives* load-bearing with nothing stating which — and it is the fifth heuristic you already declined to ship once |
| **(B)'s role field** | **not now** — one consumer, one descent. Ledgered with a trigger: **the LSP completion slice**. Then: same walk, and keyed by **position**, not by reading |
| confirmation | under (A) the legal standalone action needs **no carve-out** — a statement is not a value position. A rule whose legal cases need no exceptions is usually right |
