# `fast`, and the thing underneath finding 2

**A and D are the same question**, and answering it answers B as a side effect.
C is yes.

The short version: **finding 2 is not about `fast`, and the tested behaviour it
collides with is not a feature.** `var hidden cost => number` has two well-formed
readings and the language picks one silently — which is the failure this whole
design exists to prevent, sitting in a test that asserts it.

One caveat first, because it decides everything below: my clone is old, so §1 is
**conditional on a check you can run in a minute**. If the check comes out the
other way, A becomes "reserve nothing" and B and D go with it.

---

## 1. The check to run first

> **Is the modifier reading of `var hidden cost => number;` well-formed?**

That is: could `hidden` be consumed by `Modifiers.Parse` and `cost` be the name,
producing *a hidden variable named `cost`*?

If yes, then that source has **two readings**:

```
  (modifier hidden) (name cost)     a hidden variable called «cost»
  (name hidden cost)                an ordinary variable called «hidden cost»
```

and `Test/Unit/Loops.cs:90` asserts the second — **silently**, with no
diagnostic, and with the first unreachable. There is no bracketing that recovers
it: you could never write *a hidden variable named `cost`*.

That is the `wait time` shape exactly. A name capturing a construct that has no
bracketable form, and the reason the self-ambiguity rule exists at all.

**And it is worse for `reactive` than for `hidden`**, because the two readings
differ *behaviourally*: `var reactive score => number` is either a graph node or a
plain variable, and which one you get is decided by a parse order nobody chose.

The test's comment says *"a modifier in this position announces nothing."* That
conflates **announcing a production** — what `if`, `function`, `type` do — with
**meaning something here**. A visibility or reactivity modifier on a declaration
means a great deal. The comment is right about the mechanism and wrong about the
consequence.

If the modifier reading is *not* well-formed there — if some grammatical reason
makes `Modifiers.Parse` unable to apply — then there is one reading, the
allowance is harmless, and none of §2–§4 applies. **Check before acting.**

## 2. Question A — refuse a modifier at a name head, for every modifier

Assuming §1 confirms: **neither option you posed.** Not `fast` specifically, and
not "reconsider the allowance" as an open question — **retire it.**

Your inference (that `fast` is special because `fast number` shadows a *type
spelling*) is careful, and I do not think it is the distinction. Under
`TYPEHALFRULINGS` §1 there is **one number type**, so `fast number` is not a type
name and `type of x` never yields it — the value name `fast number` collides with
nothing in the type kind, and the kind filter would keep them apart even if it
did. **`fast` is not special. Every modifier has the same defect.**

Cost, measured — names that would stop being declarable because they begin with a
modifier word:

```
  hidden          38    0.012%
  reactive         0    0.000%
  fast            77    0.025%
  shared          97    0.031%
  constant        55    0.018%
  external        85    0.028%
  open           333    0.108%      ← may not be one of yours
  union          685    0.222%      (0.114% without «open»)

  for scale: «old» 0.137%, «return» 0.058% — both accepted
```

The set is a guess; the real one is `Lexicon/Modifier.cs`, so **re-measure with
it**. But at roughly the price of `old`, which we took, and for a refusal that
**restores a meaning currently unreachable** rather than merely preventing a
confusion — that is a good trade, and a rare one.

## 3. Question B — the check in the grammar, the docs from the modifier set

**Neither (a) nor (b) as posed.** Your instinct that *"a modifier is honestly
neither"* value nor type is right, and inventing `SymbolKind.Reserved` to make
the documentation come out is the tail wagging the dog — a dishonest table entry
to satisfy a generator.

Take **(b) for the check**, and fix the documentation the way the descriptor
slice already established:

> **The registry generator reads the modifier set as a second source.** One fact,
> several consumers — not one fact copied into a registry that has no honest
> place for it.

`docs/reference.md` and `docs/reserved-words.txt` then list every modifier
because the modifier set is where modifiers live, which is the audit's actual
complaint answered without the fake kind.

And the objection you raised against (b) dissolves: the message does **not** have
to be *"expected identifier"*. `Name.Parse` knows exactly why it refused, so it
can say *«fast» begins a modifier, and a name may not start with one — write
«pace» or «fast pace»*. A grammar-level refusal is allowed to be as informative
as a table-level one; it just has to be written.

## 4. Question D — yes, and it is A, so write them as one

Modifier **placement** — `fast if true`, `hidden while`, `fast type box` — is a
general modifier question and should not be carried by `fast`. Agreed.

But it is not a *separate* concern from A. Both are the same unasked question:

> **Where may a modifier appear, and what does it mean there?**

`fast if true` compiles because nothing asks the second half. `var hidden cost`
picks a reading because nothing asks the first. One write-up, covering: which
modifiers apply to which productions, what each means there, and what happens
when one appears somewhere it means nothing. That is a real slice and it is worth
its own document — but one document, not two.

## 5. Question C — yes, with a ledger row

`fast`'s target and duplicate validation is checker work, for exactly the reason
you give: knowing the annotation resolved to `truth` rather than `number` *is*
the resolved semantic type finding 1 says does not exist.

Add the addition we agreed for the base gap: not "tagged", but an **expiry-ledger
row with its successor named** —

```
  gap                            approximates        becomes
  «fast» on a non-number, and    no check at all     a target/duplicate check
  duplicated «fast», compile                         at the typed occurrence
  cleanly                                            (finding 1's checker)
```

An entry that says only *deferred* produces a rediscovery.

## 6. On the audit itself

Finding 1 is correct and worth acting on, but it argues against a claim nobody
made: your handoff described this as the annotation-resolution foundation, and
the audit's own *"what the implementation gets right"* section says the same
thing in the same words. **Read it as a scope statement, not a verdict** — the
list of what is still open is the useful part and it is accurate.

Findings 4 and 5 were both real and both yours to fix, and you did. Finding 6 as
a whitespace-only commit is the right disposition.

## 7. Summary

| | |
|---|---|
| **check first** | is the modifier reading of `var hidden cost` well-formed? If no, §2–§4 do not apply |
| A | **refuse a modifier at a name head — every modifier, not `fast`.** The allowance is a silent pick between two meaningful readings, one of which is unreachable |
| why not `fast`-specific | one number type, so `fast number` is not a type name and collides with nothing. **`fast` is not special** |
| cost | ~0.11–0.22% on a guessed set — about `old`, which we took. **Re-measure from `Lexicon/Modifier.cs`** |
| B | **(b) for the check** — no `SymbolKind.Reserved`, your honesty instinct is right. **The generator reads the modifier set** as a second source, which fixes the docs without a fake kind |
| the (b) objection | dissolves — `Name.Parse` knows why it refused and may say so |
| D | **yes, and it is A.** One question: where may a modifier appear and what does it mean there. One document, not two |
| C | **yes**, with an expiry-ledger row naming finding 1 as its successor |
| the audit's finding 1 | correct, and arguing against a claim you did not make. Its open list is the useful part |
