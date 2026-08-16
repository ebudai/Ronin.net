# Q4 and Q5 — enforced uniqueness, shape the value, distinguish, and a typed root

> **Ledger** — `[R]` Q4 and Q5 — enforced uniqueness, shape the value, distinguish, and a typed root
> supersedes: not yet checked
> superseded by: not yet checked

**Q4a: enforced object uniqueness. Q4b: shape the value now — my wording was
ambiguous and the audit is right. Q5a: distinguish. Q5b: the module-identity
type**, plus one requirement on the buffer identity that is the span lesson
arriving a third time.

---

## Q4a — enforced uniqueness, and it is not a compromise

The dilemma dissolves once you ask what I actually objected to. My words were
*"a slot on the variable, not an external map"*, but the **reason** was:

> a `Variable` handed to another pass with an external map **carries no way to
> ask what it requires** — the caller must also be handed the map, and its
> lifetime and ownership are unspecified.

Enforced uniqueness satisfies that. A shared cell *referenced by* the variable
would also satisfy it — but only if two independently-constructed `Variable(7)`
get the *same* cell, and the only ways to arrange that are a factory or a
construction-time map, which is a factory with extra steps. **The shared-cell
option collapses into the uniqueness option at construction.**

And the cost you name is not a cost:

> *"`new Sort.Variable(n)` can no longer be free; construction sites route
> through the factory."*

**That is the point, and it fixes a second latent bug.** An inference variable's
identity is the engine's to *mint* — freshness is the whole property. A public
constructor that hands out `Variable(7)` to anyone already permits two passes to
mint 7 and mean different variables, requirements or no requirements. Routing
construction through the supply is what every inference engine does, for that
reason.

The general form, which is why this is the right shape rather than the tidier
one: **make the invalid state unconstructible.** Equal values with independent
mutable state is the invalid state; a factory means nobody can build one.

## Q4b — shape the value now. My "empty" meant the machinery, not the type

The audit is right and the tension is my wording's fault. I wrote *"an explicit
requirements handle now, empty, without the constraint machinery behind it."*
**"Without the machinery" meant do not build the solver.** It did not mean leave
the element type as a stand-in — and read the other way it contradicts the very
thing Q1's addition existed for:

> *"shape the case so it can grow one **without a rewrite of every construction
> site**."*

An element type that must be replaced *is* that rewrite. `ISet<Pattern>` cannot
carry what `GENERICSII` §5 defines a requirement to be — *a pattern resolving for
a **tuple** of types*, with provenance. `max of (a) (b)` is one operation over a
**pair**; a set of bare patterns loses which operands, and loses the site that
induced it, which is precisely what the call-boundary diagnostic must name.

> **Define the requirement value now: the pattern, the participating type terms,
> and the provenance.** Three fields and no solver behind them.

That is not constraint machinery; it is the shape the machinery will fill. And
one consequence to build in: it is a collection of **requirement records** deduped
whole, not a set of patterns — two requirements sharing a pattern over different
operands are two requirements.

## Q5a — distinguish, and not narrowly

Your lean is right, and there is a stronger reason than the contract.

**The always-running IDE is the premise.** An unsaved buffer is not an edge case;
it is the *normal* state while someone is typing a new file. Rejecting pathless
compilation means the language server cannot check a buffer until it is saved,
which contradicts *debug is development* more directly than it contradicts
`SourceText`'s contract. Not close.

## Q5b — the module-identity type, and my own prohibition is the argument

Take the typed root. The reason is the rule I gave in `CONTAINERIDENTITYRULING`
§3: *nothing may parse a rendered identity back*, which I asked to hold **by
construction rather than by discipline**.

An opaque identity string in the same slot as a path is a discriminated union
encoded in a string — the slot means *either* a path *or* a token, distinguishable
only by convention, which is an invitation to inspect it to find out which. The
two namespaces are also shared, so a synthetic token and a real path can in
principle collide.

**And the type earns its keep twice.** The ledger already says module identity
becomes a **declared module name** when modules acquire one. Under a string that
is a third meaning in one slot; under a type it is a third case:

```
  ModuleIdentity = Path(string) | Buffer(identity) | Named(name)   ← the successor
```

The smaller change is smaller once. The type is right three times.

### The requirement on the buffer identity — the span lesson, third time

A per-`SourceText` **object** identity is not enough, and this is worth catching
before it is built:

> If a new `SourceText` is created per keystroke, a buffer's module identity
> changes on every edit — so every named type in an unsaved buffer changes
> identity, and the cache invalidates wholesale. **That is the span defect
> again**, in the one place the always-running premise cares about most.

So: **the buffer identity must belong to the editor's document, not to the
snapshot.** One stable token per open buffer, minted when the buffer opens and
surviving every edit until it is saved and acquires a path.

Whether the language server already keeps a stable document handle is a question
about the tree and therefore yours. If it does not, that handle is the thing to
add — not a `SourceText` field.

## On finding 2, which you are building without a decision

Agreed that it needs none, and one thing worth knowing while you build it: **the
fix is safe *because* of the B ruling.**

Making a function's signature see its body-local types is what H-wide means, and
it is the case I flagged as a circularity risk when refusing option A — *identity
→ resolution → identity*, because A's container carried resolved parameter sorts.
Under B the container is the shape words, so a signature may name a body-local
type with no cycle at all. The two rulings interlock; the second is what makes the
first buildable.

## Summary

| | |
|---|---|
| **Q4a** | **enforced object uniqueness.** The shared cell collapses into it at construction anyway, and routing through a supply fixes a second bug — a public constructor lets two passes mint the same identity |
| the principle | **make the invalid state unconstructible**, not merely detectable |
| **Q4b** | **shape the value now.** My "empty" meant *no solver*, not *a stand-in type* — and a stand-in is exactly the rewrite Q1's addition existed to prevent |
| the value | **pattern, participating type terms, provenance.** A collection of records deduped whole, not a set of patterns |
| **Q5a** | **distinguish.** An unsaved buffer is the normal state under *debug is development*; rejecting it contradicts the premise, not just the contract |
| **Q5b** | **the module-identity type.** A string slot holding either a path or a token is a union encoded in a string — and the ledgered **declared name** makes it a third case rather than a third meaning |
| the catch | **the buffer identity must belong to the editor's document, not the snapshot** — otherwise it changes per keystroke and the span defect returns |
| finding 2 | needs no decision, and is **buildable because of B** — under A it would have been circular |
