# Anchor capture — verified on the compiler, and it is one law in two costumes

> **Ledger** — `[R]` Anchor capture — verified on the compiler, and it is one law in two costumes
> measured at: 4ccfddc

Answering `NAMEVSANCHOR.md`. §2's finding reproduces exactly on `Resolver.cs`,
and R6b closes half of it. The other half is a question already open.

## 1. §2 reproduces, including the propagation

```
patterns «print (_)», names {a, nothing}          before declaring «print a»   after

  print a                     Resolved cost=2  print «a»          cost=1  «print a»
  nothing otherwise print a   Resolved cost=3  … otherwise print «a»
                                                                  cost=2  … otherwise «print a»
```

`Resolved` both times, never `Ambiguous` — the name is strictly cheaper, so it is
silent, and the capture propagates into any expression containing the call.

The mechanism is the width of the pattern, not its spelling. A two-word
anchor-only pattern goes the same way:

```
  sum of a                    cost=2  sum of «a»          cost=1  «sum of a»
```

## 2. §3's R5-protection claim also reproduces

```
patterns «send (_) to (_)», declaring «send queue»

  send queue to a             cost=3  send «queue» to «a»   unchanged
```

The name cannot reach the whole call because it would have to span the
mandatory `to`, so the glue-bearing pattern really is protected — for the reason
given, and not by accident.

## 3. The same law, with an operator instead of a pattern — and R6b misses it

```
names {x, y}                                       before declaring «x otherwise y»   after

  x otherwise y               cost=2  («x» otherwise «y»)   cost=1  «x otherwise y»
```

Identical shape, identical silence, identical cost step. This is the finding I
reported when `otherwise` was built (`Test/Unit/Fallbacks.cs`, *"a declared name
takes the words back, silently"*), and R6b as stated does not reach it: there is
no pattern, so there is no "entire word content of a visible pattern" to be a
prefix of.

So the root is one sentence, and it is about the cost model rather than about
patterns:

> **A name is one lookup. Any composite reading of the same span is at least
> two. So a name whose span equals a composite's span always wins, silently.**

R6b closes the case where the composite is an anchor-only pattern call. The case
where the composite is an *operator expression* is the open `otherwise`
question — the one `docs/spec/grammatical-structure.md` §4.6.7 currently records
as undecided. They are the same defect and I would decide them together, because
deciding one and not the other leaves the registry telling half a story.

The two differ in what a fix costs:

| composite | name that shadows it | what a rule would reserve |
|---|---|---|
| anchor-only pattern call | begins with the pattern's words | a **name prefix** — R6b, measured cheap |
| operator expression | **contains** the operator word | the operator word inside any name — R5's bill, which is why R5 exists |

The second is the more expensive rule, which is presumably why `otherwise` was
left open. Worth noting that it is *already* R5's shape: glue words may not be
names, and an infix operator word is glue in everything but name.

## 4. On R6b itself

Sound as far as I can check it, and the "proper prefix" wording is doing real
work: a name **equal** to the pattern's word content — `print` where `print (_)`
exists — cannot capture, because `print a` would then be two juxtaposed names
and is not a reference at all. Excluding it is correct and not merely lenient.

Implementation is where R6 already lives: `Rules.Anchors` compares patterns
pairwise against the merged table and reports the later declaration. R6b is the
same walk with names on one side, and the finding shape — `AnchorPrefix` with an
`Alongside` for the collision site — carries over.

## 5. The registry item, which is small and I can do

> `RESERVED (0)` is still a true word count. It is not a complete account of
> what patterns cost names, and the registry should say so.

`docs/reserved-words.txt` is generated and gated by a test, and it already has a
section for *"RESERVES NOTHING — every hole before a word is determinate"*. The
anchor-only patterns want the same treatment with the opposite sense: reserving
no word, and reserving their word run as a name prefix. That is a generator
change and a test row, and it does not need R6b to land first — the statement is
true today whether or not the rule exists.

Say the word and I will add it.
