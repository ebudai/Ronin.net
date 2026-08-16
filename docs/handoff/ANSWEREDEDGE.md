# "Answered by" is a third edge, not a kind of supersession

> **Ledger** — `[V]` An answered memo is **not** superseded; the relation gets its
> own paired `answers`/`answered by` field, filled in Pass 1 because the pairs are known.
> answers: the programmer's Pass-1 flag on the ~20 memo→ruling pairs
> supersedes: LEDGERRULING §6–§7
> superseded by: none

**You are right, and your Pass-1 reading was the correct safe one.** An answered
memo is not superseded. But leaving those ~20 pairs at `not yet checked` is also
wrong, and for a reason that makes this cheaper rather than dearer: **the relation
is known, so it belongs in Pass 1, not Pass 2.**

You also caught a real error in my own header, which is the failure mode of
teaching a convention by example. `LEDGERRULING` should have carried
`answers: LEDGERBACKFILL`, not `supersedes: nothing`.

---

## §1 — why it is not supersession

Take `SEMANTICCHECKERSCOPING` → `CHECKERSCOPINGRULINGS`. Is the package dead?

- It was never binding — it is `[R]`, and that is unchanged.
- Nothing in it was struck. Most of it was **confirmed**.
- The ruling does not restate its reasoning; it assumes you have read it.

So the package is still where the argument lives, and a reader sent away from it
loses the reasoning behind a decision they are trying to apply. **Supersession
means *do not rely on this, rely on that instead*** — and that is false here.

The decisive form, because it is what the two edges tell a reader to *do*:

| edge | what the reader should do |
|---|---|
| `superseded by X` | read **X instead** |
| `answered by X` | read **X as well** — and X is where the verdict is |

Those point in opposite directions. Conflating them makes one reader discard a
live document and another miss the ruling that governs it. That is precisely the
argument §6 made for supersession being a field rather than prose, arriving one
level in.

## §2 — the ruling

> **Add a paired edge, `answers` / `answered by`, distinct from supersession.**
> A consultation package carries `answered by: <ruling>`; the ruling carries
> `answers: <package>`.

**`answered by X` means: X governs.** That holds uniformly whether the ruling
confirmed the memo, modified it, or rejected it outright — a reader who follows
the edge gets the truth in every case, so *"answered and rejected"* does not need
a fourth state. Shades stay in the prose, as decision 1 says.

Supersession keeps the meaning you already gave it in Pass 1: **explicit strike,
withdraw or replace**. Your encoding was right; it was only missing somewhere to
put the other relation.

**And this moves work out of Pass 2, not into it.** The memo→ruling pairs are
known — they are the relay record, not archaeology. Filling them in Pass 1
converts ~20 documents from *"nobody has walked this"* to a stated fact, which is
20 fewer entries on Pass 2's generated worklist. Pass 2 keeps only the genuine
archaeology: which document struck which claim in which other document.

## §3 — and it gives the generator its first job

The edge is **paired**, which means it is checkable: every `answered by: X` must
have a matching `answers:` on X, and vice versa.

> **The index generator's first job is verifying edge reciprocity.**

That is what turns the ledger from a hundred hand-written claims into something
that stays true — a dangling or one-sided edge becomes a visible failure in a
generated artefact rather than a quiet inaccuracy in a file nobody opened. It is
the concrete payoff of the *"a fact with no consumer cannot be kept true"* rule,
and it costs one pass over the corpus.

Worth doing the same for supersession once Pass 2 fills it: `supersedes` and
`superseded by` should reciprocate too.

## Summary

| | |
|---|---|
| the call | **an answered memo is not superseded.** Your Pass-1 reading was right |
| but | `not yet checked` is also wrong — the relation is **known**, so it belongs in **Pass 1** |
| the fix | a paired **`answers` / `answered by`** edge, distinct from supersession |
| the test | `superseded by X` = read **X instead**; `answered by X` = read **X as well**, and X governs |
| uniformity | `answered by` holds whether the ruling confirmed, modified or rejected the memo. No fourth state; shades stay in prose |
| supersession | unchanged — **explicit strike, withdraw or replace**. Your encoding stands |
| the effect on Pass 2 | **smaller.** ~20 pairs leave the worklist; Pass 2 keeps only real archaeology |
| the generator | **first job is edge reciprocity** — every `answered by` has a matching `answers`. A dangling edge becomes a visible failure |
| my error | `LEDGERRULING`'s own header should have read `answers: LEDGERBACKFILL`. Teaching a convention by example taught the wrong thing; the example is corrected here |
