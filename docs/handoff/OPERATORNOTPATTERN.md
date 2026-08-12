# Both corrections accepted — and the machinery has one rule I would get wrong by default

His R7b restatement is better than mine and I would take it. But I checked the
thing that worried me rather than assuming it, and there are two additions:
**don't delete the pattern half of the relation**, and **the multi-word scan
must not be greedy.**

---

## 1. The conclusions survive the pattern → operator move — measured

Every probe I ran modelled the family as patterns. Moving them to the operator
table changes every cost — a pattern call costs one lookup, an operator costs
none — so "it shifts both sides equally" needed checking rather than asserting.

`word_infix_ops.py` §1, with `is`, `is not`, `is a`, `is not a` as real word
operators:

```
  x is y          + name «x is y»     OK 2 -> OK 1            CAPTURE
  x is a number   + name «a number»   OK 2 -> TIE -> ERROR    tie
  x is not x      + name «not x»      OK 2 -> TIE -> ERROR    tie
  x is valid      + name «is valid»   OK 2 -> OK 2            unchanged
```

Same verdicts, one lookup cheaper on both sides. `is valid` stays legal, the
three hazards stay hazards. **R5′ and R7b carry over unchanged.**

## 2. His restatement is better — but it should not replace the pattern half

Over operator word-runs, prefix-extension gives the same set by a simpler route:

```
  operators: [is, is a, is an, is not, is not a, is not an]
  R7b from prefix-extension = [a, an, not]
```

Deriving it from `Builtin.Operators`, which already generates `Rules.Infix`, is
strictly better than my hole-insertion definition for this family.

**But the hole-insertion relation is still live for patterns.** It just does not
apply to a family that is not patterns:

```
  sum of all things   without «all things»       OK            sum of all «things»
  sum of all things   with the name «all things» TIE -> ERROR
```

`sum of all (_)` refines `sum of (_)` by inserting `all` at the start of its
hole, and the tie appears exactly as it does for the operators.

So R7b has **two sources feeding one set**:

| source | relation | example |
|---|---|---|
| operators | prefix-extension of a word run | `is` → `is not` → `is not a` |
| patterns | insertion at the start of a hole | `sum of (_)` → `sum of all (_)` |

Today the stdlib has no such pattern pair, so an operators-only generator gives
the right answer — and will keep giving it until someone adds one. That is
"correct by coincidence" again, and it is cheap to avoid by generating from both
tables now while both generators are being written anyway.

## 3. The machinery rule I would have got wrong: do not match greedily

This is the most actionable thing here. The obvious multi-word scan is
longest-match at each position. It is wrong:

```
  x is not a number   without «a number»         OK            («x» is not a «number»)
  x is not a number   with the name «a number»   TIE -> ERROR  («x» is not «a number»)
```

A greedy scan takes `is not a` in **both** rows and never reports the second.
That is a silent pick, decided by the matcher rather than by cost — the exact
defect R7b was derived from, reintroduced by the machinery built to serve it.

> **At each position, offer every operator run that matches. Do not commit to
> the longest. Cost decides; ties are errors.**

Which is the language's existing rule, applied one layer down. R7b then keeps
the ties from arising in the first place, and the two mechanisms agree instead
of one hiding the other.

## 4. On the B re-estimate — the machinery is not optional

He is right that "registration and binding power" was wrong for the multi-word
forms, and I would go further: **there is no cheaper reading available.**

- `is not` cannot be compositional. `x is (not y)` means *x equals the negation
  of y* — a different question, and an error for non-truths. It is not
  inequality.
- `is a` / `is an` have no single-word spelling at all. The article is the
  namespace selector (`IS-AND-EQUALITY.md` §9); there is nothing to collapse.

So multi-word infix is forced by `is a` regardless of what happens to `is not`,
and `is not` gets it for free once built. The question is not whether but how B
splits — and I think the shared machinery wants its own row rather than living
inside B:

| | depends on | notes |
|---|---|---|
| **A** R5′, R6b, R7b (generated from operators **and** patterns) | — | unchanged |
| **B₀** bare `is` | A | genuinely registration + binding power |
| **B₁** multi-word infix in the operator scan | A | machinery; **offer all runs, not greedy** |
| **B₂** `is not` | B₁ | |
| **C** `is a` / `is an` | B₁ + type table | |

B₁ is a shared prerequisite of B₂ and C, and it is reusable by every future word
operator — worth knowing when sizing it, because the cost is paid once for the
whole class rather than for this family.

`otherwise` being a single-lexeme operator means there is no precedent in the
tree for the multi-word case. This is new work, not an extension of existing
work, and estimating it as the latter is how B got mis-sized the first time.

## 5. Summary

| | |
|---|---|
| `(_) is (_)` cannot be a pattern | **accepted** — leading hole, and `otherwise` is the precedent |
| R7b restated over operator word-runs | **accepted, and better** — same set, derives from `Builtin.Operators` |
| replace the hole-insertion relation | **no** — still live for patterns (`sum of (_)` / `sum of all (_)`). Generate from both tables |
| do the earlier conclusions survive | **yes, measured** — same verdicts one lookup cheaper |
| multi-word scan | **offer every run, never greedy.** Greedy hides the exact tie R7b exists to prevent |
| B was mis-estimated | **agreed**, and `is a` forces the machinery regardless — split B₁ out as its own row |

Probe: `word_infix_ops.py` — a working multi-word word-infix DP resolver, the
four carry-over cases, the greedy-vs-offer-all demonstration, and the pattern
half of the refinement relation.
