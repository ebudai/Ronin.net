# Consultation — modelling value positions for action admissibility

> **Ledger** — `[R]` Consultation — modelling value positions for action admissibility
> answered by: VALUEPOSITIONSRULING
> supersedes: none
> superseded by: none

`REAUDIT79` is right, and it is the third audit on one rule — *an action is admitted in
no value position* (`FIVE-RULINGS` §2b). REAUDIT77 found operator operands; REAUDIT78
found untyped peers, `otherwise`, and inferred aggregates; REAUDIT79 finds generic call
arguments, omitted-return answers, untyped initializer roots, and round-group parts. Each
fix has closed the named routes and revealed the next. **That pattern says the approach is
wrong, not the patch** — so before shipping a fourth, I want your ruling on the model.

## What a value position is

The complete set, as best I can name it: an operator's operands; a call's arguments
(whether the parameter has a written type or is generic); a list's elements; a lookup's
keys and values; a round group's parts (transparent); a `return (_)`'s answer; and a datum
initializer. **Not** a standalone `act 1;` statement — performing an action is legal.

## Why one uniform walk does not fall out

Admissibility is currently split across two mechanisms that meet badly:

1. **`Disagreeing`** (called by `Arguments`, `Returns`, `Initializers`) emits `ActionInValue`
   when a value's inferred sort is an action — but only where there is an **expected sort**
   to compare against. It walks the GRAMMAR value tree, so it reaches a typed list's
   elements structurally. It never fires for a position with no expected type.

2. **`Inadmissible`** (the REAUDIT78 fix) walks the RESOLVED tree for actions in operator
   operands and list/lookup entries, independent of any expected type.

Extending #2 to the REAUDIT79 routes runs into two facts I verified by execution:

- **They double-report.** A typed call argument `send (act 1)` is caught by `Arguments`
  (via `Disagreeing`) AND by an `Inadmissible` that walks call arguments — at **different
  spans**: the resolved argument is a keyed group `⟨act 1⟩`, so one finding lands on the
  group and the other on the inner `act 1`, and dedup (which is by exact span) keeps both.

- **A reading's root is not a value position it can see.** Readings are the OUTERMOST
  references of a statement. For `var xs => list of number = [act 1]` the reading is
  `«act 1»` — the element — because a `[…]` collection is not itself a reference; the list
  never appears as a node a resolved-tree walk could descend. So a walk that checks a
  reading's *children* misses it, and it cannot simply check *roots*: `var r = act 1`
  (illegal) and `act 1;` (legal) produce the **same** reading, `«act 1»`. The resolved tree
  alone does not carry which one it is.

The root cause is that admissibility is a **grammar role** — "this value is used as X" — and
the resolved reading, an outermost reference with no role attached, is the wrong thing to
drive it from.

## The question, and three ways I can see

**How should the checker model value positions?**

- **(A) One grammar-driven admissibility walk.** Walk the statements, not the readings:
  a datum initializer and a `return` answer are value-position roots; from each, descend
  recursively through operands, call arguments, list/lookup entries, and transparent round
  groups, reporting an action at each. Replace `Disagreeing`'s `ActionInValue` entirely, so
  there is one source. *Cleanest model; the largest change — it unifies the grammar-tree and
  resolved-tree checking that today are separate.*

- **(B) Give a reading its role.** Have `Read` tag each reference with the grammar position
  it sits in — initializer, argument, element, operand, return-answer, or a bare statement.
  A single resolved-tree walk then checks a reading's root when its role is a value, and its
  children always. *Smaller — a field on `Reading` and context in one walk — and keeps the
  resolved-tree checking; the readings model gains the fact it is missing.*

- **(C) Keep the split, dedup the overlap.** Leave `Disagreeing` for typed positions, add
  `Inadmissible` for the untyped gaps only, and dedup `ActionInValue` by span CONTAINMENT
  rather than equality. *Smallest, but it entrenches the two-mechanism split the treadmill
  came from, and containment-dedup is a new rule with its own edges.*

**My lean is (B):** it gives the auditor's "complete set … modelled as a set" with the
least architecture disturbed — the readings carry a role they arguably should have anyway,
and admissibility becomes one walk over that. (A) is the more principled end state if you
would rather the checker stop having two trees; (C) I would only take under time pressure,
and the treadmill argues against it.

Whichever you rule, the acceptance targets are `REAUDIT79`'s controls: generic and typed
call parameters, written and omitted returns, typed and untyped initializers, singleton and
multi-part round groups, list/lookup entries, and a standalone action statement that stays
clean. Nothing is built against this yet; the branch is at the clean `bed1acc` baseline.
