# The trim is right about coverage and wrong about reason — keep `not` in the generated set

Measured both ways. His hypothesis holds for the outcome and fails for the
mechanism, and the difference is the shape this audit round has already turned
up three times.

Also: **start A now.** Do not wait for the square-aggregates document.

---

## 1. He is right that R6b covers it — today

```
  is family only    R7b generated = [a, an, not]
                    R6b anchor-only word runs = []
  + «not (_)»       R7b generated = [a, an, not]
                    R6b anchor-only word runs = [not]
```

Exhaustive sweep, names beginning each word, R5′-refused names excluded:

```
  is family only, no «not (_)»
      names beginning «a»    break something   (covered by R6b: False)
      names beginning «not»  break something   (covered by R6b: False)

  with «not (_)» declared
      names beginning «a»    break something   (covered by R6b: False)
      names beginning «not»  break something   (covered by R6b: True)
```

So with `not (_)` in the pattern set, trimming R7b to `{a, an}` changes no
outcome. His instinct to confirm that by execution rather than assume it is
exactly right — and it is the discipline I have failed at three times in this
round, so it is worth saying that it is the right call and not just a cautious
one.

## 2. But the hazard exists without `not (_)`, and that is the whole point

The top-left cell is the one that matters: **with no `not (_)` pattern at all**,
names beginning `not` still break things. The tie comes from `(_) is not (_)`
refining `(_) is (_)` at a hole boundary — a property of the `is` family alone.

So trimming makes the safety of `(_) is not (_)` **contingent on an unrelated
pattern continuing to exist.** Respell negation as a symbol later, or drop
`not (_)` for any reason, and the hazard returns with no rule watching it.

That is "invariant enforced by coincidence" again — the same shape as the
immutability invariant, the `O(n)` comment, and `MaxGroups`' brace-specific
explanation. It costs nothing to avoid here: **generate the set from the
refinement relation and let R6b and R7b overlap on `not`.**

## 3. The generator, so it is code rather than a description

`r7b_generator.py`:

> For patterns *P* and *Q*, *Q* **refines** *P* at a hole boundary if *Q* is
> *P* with one or more literal words inserted immediately at the start of one
> of *P*'s free holes. The **first inserted word** is an R7b word.

Over the `is` family that yields `{a, an, not}` in both configurations.

The empirical sweep finds `{a, not}` — `an` is absent only because the test
universe has no `an` token, so the generated set is a superset by exactly the
words the universe cannot exercise. That is the right direction for a
conservative generator to err, and worth a note in the registry so nobody
"fixes" the difference later.

## 4. His diagnostic worry is real and has a better fix than trimming

> *"two rules overlapping on the same word and a reader having to work out
> which fired"*

Correct, and the fix is to stop reporting rules. **Report the causes:**

```
  «not x» cannot be declared:
      «not (_)» would be shadowed by it
      «(_) is not (_)» refines «(_) is (_)», so a name beginning «not» can
      absorb the difference
```

Rule names are for us; pattern names are for the programmer. And two causes is
*information* rather than confusion — it tells them that removing one pattern
will not free the name, which a single rule name actively hides.

## 5. Slice A, as specified

Agreeing with his three lines, with one change to the third:

- **R5′** — narrow `Infixes(names)` from *contains a glue word anywhere* to
  *interiorly*.
- **R6b** — unchanged; starts biting when `not (_)` is declared.
- **R7b** — **generated from the refinement relation**, currently
  `{a, an, not}`. `a`/`an` conditional on the remainder resolving as a type;
  `not` blanket. Overlap with R6b is expected and reported as two causes.

Budai's call on `not` — blanket, "who names something starting with `not`" —
stands and is unaffected by any of the above.

## 6. Start A now

**Do not wait for the square-aggregates document.** A is unblocked, it must
precede B, and blocking a clean grammar slice on my writing is the wrong
dependency direction. He also noted himself that E will read better against a
settled `is` — which is an argument for A first, not for E first.

I will write E while A+B₁ is in flight, and it will be better for having the
`is` slice landed to build against.

Probe: `r7b_generator.py` — the refinement generator and the exhaustive
cross-check, both configurations.
