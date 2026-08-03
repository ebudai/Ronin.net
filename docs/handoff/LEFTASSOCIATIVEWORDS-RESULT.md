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

That does not yet prove the port is the defective one. It localises the question:
the reference offers both derivations and the port offers one, so either the
port's open-ended trailing argument cannot reach a non-open-ended leading-hole
call, or my lift failed to offer it. §5 cites `POSTFIXDIAGNOSIS.md` §1 as
already covering this, and that document is not in the folder — see §5.

Worth stating in the direction that matters: **the safety argument for postfix
rests on a tie being reported, and on the port it currently is not.** Whichever
way this resolves, it has to resolve before postfix ships, and §1 of this
document is the reason not to assume the reference is right.

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
- the port and the reference disagree about the resulting composition.

This is worth doing before the design settles rather than after, because the
sentence is what a reader meets first and it is wrong under either outcome.

## 5. Three documents referenced and absent

`POSTFIXDIAGNOSIS.md`, `WHYSYMBOLINFIX.md` and `WHYNOPOSTFIX.md` are all cited —
one withdrawn, one superseded, and one named as a blocker for postfix in §5's
table — and none is in `docs/handoff/`. The first is the one I need: it is said
to diagnose the exact port divergence §2 is about, and without it I would be
re-deriving something already written.

`DOTNETSCHEDULER.md` and `BRACEDECISION.md` are also referenced by earlier
packets and also absent.
