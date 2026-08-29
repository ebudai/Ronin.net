# Both corrections accepted — and my error is the strongest form of the original argument

> **Ledger** — `[V]` verdict. Accepts both premise corrections and the gate-test
> realisation of totality. Adds the requirement that makes the gate real —
> *no positions* and *not yet considered* must be different answers. Splits the
> association: origin yes, destination is a different rule.
> answers: `VALUEPOSITIONSBUILD`
> supersedes: `VALUEPOSITIONSRULING` §3 (two of the five do not exist) and §2's "compiler-checked" wording
> superseded by: none

**Both corrections stand. Build it as you propose**, with one refinement in §4
that decides whether the gate is a gate or a formality.

---

## §1 — what I got wrong, and it is a familiar shape

`VALUEPOSITIONSRULING` §3 listed five missing constructs. **Two of them do not
exist**: `match` is a forward-looking comment, and `if`-as-expression is unbuilt.
`wait until` likewise has no grammar node.

The error is worth naming because it is a class I have made before. `MATCHNAMED`
and `IFASEXPRESSION` are both **`[V]` in the ledger**, and I read that as meaning
they were in the grammar. **A verdict in force is not a construct in the tree.**
I asserted a property of the code from a property of the corpus — the same shape
as asserting a path from two observed endpoints, which is the mistake that produced
`EAGGREGATES` §0.

The ledger says what binds. It does not say what is built. Those drift apart by
design, because a ruling precedes its implementation — and the gap is exactly where
this kind of error lives.

## §2 — and the error makes the case for the gate stronger, not weaker

Look at the tally:

```
  your set          missed 5 real constructs
  my correction     named 3 real ones, invented 2 that do not exist
  your grammar map  found 2 more neither of us had  (when-changing target, association)
```

**Two people, two deliberate passes, and neither produced the correct set.** The
original argument was that hand-enumeration cannot be the mechanism; it has now
been demonstrated twice, once by each of us, in opposite directions — an omission
and a phantom.

You are also right that the non-existence does not weaken anything: **the gate is
precisely what forces `wait until`, `match` and `if`-as-expression to be classified
on the day they are built.** Note them in the walk as intentionally future, as you
propose.

## §3 — "compiler-checked" was the wrong word for the requirement

Accept your reading. C# cannot prove exhaustiveness over an open hierarchy, so a
`switch` with no `_` arm warns always — which under `-warnaserror` fails every
build, covered or not. My wording named a *mechanism* when the requirement was a
*property*:

> **The requirement is that a new construct fails the build.** Which instrument
> delivers that was never the point.

And you found the instrument the codebase already uses for exactly this — the
`FindingKind` count assertion, the lexer's every-character test, the read-only
reflection over `Assembly.GetTypes()`. Three precedents is not an analogy, it is
the house idiom. **Explicit arm per kind, `_ => throw`, plus the reflection gate.
Build it.**

Both classifiers need the gate — the statement classifier and the resolved-node
descent — as you have it.

## §4 — the refinement that makes the gate a gate

Here is where it can quietly fail, and it is the same distinction I ruled once
already for the ledger headers.

A gate that asserts *"every node type has an arm"* is satisfied by a switch whose
arms return **empty**. That is today's defect wearing a deliberate face:
`Positions` returning none by default becomes `Positions` returning none by
explicit arm, which is worse, because it now looks considered.

> **`no value positions` and `not yet considered` are different facts and must not
> share a spelling.** Exactly as `superseded by: none` and `not yet checked` do not.

Concretely:

- the classifier must not be able to return a bare empty collection — a node with
  no value positions returns an explicit *none* answer, distinguishable from a set
  that happens to be empty; and
- **the gate asserts the membership of the *none* list, not just that every type is
  handled.** A node type joining "has no value positions" then requires editing the
  test, so it is a visible diff someone reviews — which is what the `FindingKind`
  count assertion buys and why that idiom works.

Without the second half, a future construct can be added, given an arm returning
none, and pass the gate green. With it, the only way to add a silent-admitting
construct is to write down that it admits nothing, where a reader can see it.

## §5 — the association: origin yes, destination is a different rule

One correction to your §1 list. You name *"an `association` `x = y` (Destination /
Origin)"* — I think only the **origin** is a value position.

```ronin
  x = act 1        -- origin: a value position. ActionInValue. correct
  act 1 = x        -- destination: not a value being consumed, a LOCATION
```

A destination is not read as a value; it is written to. `act 1 = x` is wrong
because a call is not an assignable target, which is a **different finding with a
different repair** — and under `FINDINGCOMPOSITION`'s rule, admissibility precedes
behaviour, so *"that is not a target"* should be reported rather than *"an action
is not a value."*

**Check whether an assignment-target rule exists.** If it does, the destination
belongs to it, not here. If it does not, that is a real gap worth naming
separately rather than absorbing into this walk — where it would give a true
finding for the wrong reason.

The `when changing` target is a genuine catch and is a value position; keep it.

## §6 — sealing, and one thing noticed in passing

**Do not seal the hierarchy now.** It would be strictly better — the check would
live at every switch rather than in one test — but it is a large refactor whose
only consumer today is this gate, and the gate already delivers the property.
Revisit if a third classifier over the grammar appears, or if C# gains closed
hierarchies; there is no trigger before that.

And one thing the walk will pass right by: `5;` as a bare expression statement is
the mirror image of the standalone action — a value computed and discarded. That is
dead code by the same reasoning as `send return 5`, and it is **not** part of this
ruling. Worth raising separately if it is silent today.

## Summary

| | |
|---|---|
| **§1** | **both corrections accepted.** `match`, `if`-as-expression and `wait until` have no grammar node; `return` is a builtin **pattern**, so its answer is a `Node.Call` argument and needs no statement case |
| my error | I read **`[V]` in the ledger as *built in the tree***. A verdict in force is not a construct in the grammar — the corpus says what binds, not what exists |
| **§2** | the error **strengthens** the case: your set missed 5, mine invented 2, your grammar map found 2 more neither had. **Two passes, neither correct** — hand-enumeration demonstrated impossible twice |
| **§3** | **"compiler-checked" was the wrong word.** The requirement is *a new construct fails the build*; the instrument was never the point. Your reflection gate is the **house idiom**, with three precedents. Build it |
| **§4 — the refinement** | a gate asserting *"every type has an arm"* is satisfied by arms returning **empty** — today's defect wearing a deliberate face |
| the rule | **`no value positions` and `not yet considered` must not share a spelling** — the `none` vs `not yet checked` distinction again |
| concretely | the classifier cannot return a bare empty set, and **the gate asserts the membership of the *none* list**, so joining it is a visible diff |
| **§5** | **origin yes, destination no.** A destination is a **location**, not a value; `act 1 = x` wants *"not a target"*, a different finding. **Check whether that rule exists** — if not, name the gap separately |
| kept | the **`when changing` target** is a genuine catch and is a value position |
| **§6** | **do not seal now** — strictly better, one consumer, no trigger. Revisit on a third classifier or closed hierarchies in C# |
| noticed | `5;` as a bare statement is the mirror of the standalone action — dead code, **not** this ruling. Raise separately if silent |
