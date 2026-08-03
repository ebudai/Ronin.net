# Left-associative words — §1 verified on the port, and the divergence is now pinned down

Answering `LEFTASSOCIATIVEWORDS.md`. Nothing from §3 was implemented: §5's own
table says infix is blocked on the index and on suffix-free R6, and both are
unbuilt. One thing was, and it is independent of every open decision — see §4
below.

## 1. §1's claim about the port is correct

`Resolver.cs` handles same-precedence chains, measured rather than read:

```
a + b + c        Resolved   ((«a» + «b») + «c»)
a - b - c        Resolved   ((«a» - «b») - «c»)
a + b + c + d    Resolved   (((«a» + «b») + «c») + «d»)
a / b / c        Resolved   ((«a» / «b») / «c»)
a + b * c        Resolved   («a» + («b» * «c»))
a * b + c        Resolved   ((«a» * «b») + «c»)
a + b * c - d    Resolved   ((«a» + («b» * «c»)) - «d»)
```

Left to right, unique, and precedence respected — the last three differ from
§2's reference output because this table has real precedences (`*` is 20, `+` is
10) rather than the equalised ones the reference was demonstrating with. Both are
right for their own configuration.

Recording the compliment plainly, since the document does: the port was correct
and the reference was not. That is worth keeping in view the next time the two
disagree, which is the rest of this document.

## 2. The port/reference divergence, now measured on both sides

`POSTFIXPATTERNS-RESULT.md` §2 reported that `sorted xs reversed` resolved to a
single derivation on `Resolver.cs`, and said I could not tell whether that was
the DP's structure or my experimental wiring. `word_infix.py` §3 answers half of
it, because it runs the same input on the fixed reference:

| | derivations | reading |
|---|---|---|
| `dp_resolver.py`, fixed | **2 — TIE → ERROR** | first is `(sorted «xs») reversed` |
| `Resolver.cs`, leading holes lifted | **1 — Resolved** | `[(_) reversed]([sorted (_)](«xs»))` |

Same patterns, same names, same statement, and the port's single derivation is
the reference's first. So the two really do disagree, the disagreement is not
about which reading is preferable, and `LEFTASSOCIATIVEWORDS.md` §4 — "TIE →
ERROR still" — describes the reference.

**Answered, after this was first written.** The port produces *both*
derivations. It loses them at the point where a tie is counted.

`Cell.Offer` keys derivations by `node.ToString()` — the rendered reading — and
for equal cost keeps the larger count rather than the sum. A word-pattern call
renders without brackets, so both groupings render as `sorted «xs» reversed`,
collapse into one entry, and the count stays at one. `Resolution` then reports
`Resolved` because ambiguity is `best.Count > 1`.

Confirmed by changing only the rendering: bracketing a nested call inside
`Node.Call.Render` makes the same statement come back **Ambiguous, count 2**, at
the same cost 3. Nothing else was touched.

So:

- §4's cost invariance is true of the port as well — the two derivations exist
  and cost the same;
- the tie is not reported because the tie *detector* keys on rendering, not on
  structure; and
- the reference and the port do not disagree about the grammar at all. They
  disagree about what counts as two readings.

That dedup is not a mistake. Its comment records what it fixed: two identical
patterns in a table made «take x» count two while leaving one rendering in
order, so a tie was reported between a statement and itself with no readings to
show. Keying on the rendering is sound **while distinct derivations always
render distinctly** — which holds for prefix patterns, because their nesting is
unique, and is measurably still true today:

```
f g a                Resolved   f g «a»
sum of sum of a      Resolved   sum of sum of «a»
send f a to g b      Resolved   send f «a» to g «b»
f f f a              Resolved   f f f «a»
```

Postfix is what breaks the assumption, because a call that ends in a word and a
call that begins with one compose to the same string. So this is a
**precondition for postfix rather than a live defect**, and it is a small and
well-localised one: either a call renders its nested calls unambiguously, or
derivations are counted by structure instead of by text. The first is a
one-method change and would show up in every diagnostic that quotes a reading;
the second is invisible to users and is probably what is wanted.

Either way it belongs on §8's list, and it replaces the vaguer worry in
`POSTFIXPATTERNS-RESULT.md` §2 that the DP might not reach the reading at all.
It does.

## 3. A small inconsistency in `word_infix.py`

Its §3 prose says «sorted xs reversed» "is not a tie -- it has an answer", four
lines under output reading `TIE -> ERROR`. I take it to mean the tie has a
natural left-to-right resolution and not that the resolver reports one, which is
the argument `LEFTASSOCIATIVEWORDS.md` §4 then declines. The `.md` is
unambiguous; only the script reads as though the measurement had gone the other
way.

## 4. What was implemented: the refuted claim, in the compiler

`POSTFIXPATTERNS.md` §9 asked for the left-recursion comment to be corrected in
the probe. The same sentence was in `Pattern`'s constructor and in the
resolver's index, where it read as a language constraint and was the stated
reason for a throw. Both are corrected. No behaviour changed and no test moved;
the ban stands exactly as it did, now for the three reasons that are actually
true of it:

- the index keys a pattern by its first word and a leading hole has none;
- R6 is stated over anchor runs and a leading hole's is empty; and
- a tie between the resulting groupings is not reported, for the reason §2
  gives.

This is worth doing before the design settles rather than after, because the
sentence is what a reader meets first and it is wrong under either outcome.

## 5. Three documents referenced and absent

`POSTFIXDIAGNOSIS.md`, `WHYSYMBOLINFIX.md` and `WHYNOPOSTFIX.md` are all cited —
one withdrawn, one superseded, and one named as a blocker for postfix in §5's
table — and none is in `docs/handoff/`. `POSTFIXDIAGNOSIS.md` is said to
diagnose the port divergence §2 is about; §2 now has an answer arrived at
independently, so the useful thing is to compare the two rather than to send it.

`DOTNETSCHEDULER.md` and `BRACEDECISION.md` are also referenced by earlier
packets and also absent.
