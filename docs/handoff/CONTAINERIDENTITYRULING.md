# Container identity — **B**, and the module path with a named successor

> **Ledger** — `[V]` Answers `CONTAINERIDENTITY.md`: **B**, the overload set is one
> container (A ties identity to resolved parameter sorts — unstable under a signature
> edit, possibly circular). §1 module = source path, **ledgered** (a path is a
> location; successor = a declared module name). §3 structural identity, the string
> render-only and never parsed. Finding 2 lands with the refactor.
> supersedes: not yet checked
> superseded by: not yet checked

**§2: B — the overload set is one container.** **§1: confirmed, with a caveat and
a ledger row.** **§3: confirmed.** Landing finding 2 with the refactor is right.

---

## §2 — B, because A makes identity depend on resolution

Your lean is A and the intuition behind it is sound — two bodies really are two
bodies. But A carries a consequence that undoes the ruling it is implementing.

**A requires the segment to carry the resolved parameter sorts.** So a named
type's identity becomes a function of the enclosing signature's *types*. Two
things follow, and the second is decisive:

**It can be circular.** `token`'s identity would depend on the sort of `a`, which
is a `Named` whose identity depends on *its* container. Today that bottoms out.
But under H-wide the signature is part of the function, and finding 2 shows the
hoisting walk already crosses into it — so a parameter annotation naming a
body-declared type is not obviously impossible, and if it ever is possible the
recursion is identity → resolution → identity.

**And it is unstable under exactly the edit that matters.** I chose
`(scope, name)` over the span *because identity must survive an edit*. Under A:

```
  function use (x => a) { type token; … }
  rename the parameter's TYPE from «a» to «c»
      -> the signature's sorts change
      -> «token»'s container changes
      -> every instantiation keyed on it misses
```

The span moved when you edited *above* a declaration. A moves when you edit the
*signature right beside it*. **That is a worse instability than the one the
ruling removed**, arriving in a new location.

### And B is what "named container" already means

`SCOPEIDENTITYRULING` says *nearest **named** container — a module, a type body,
or a function*. An overload set is **one named thing with several bodies**; the
container is named by its name. B is the literal reading. A would redefine the
term to "named-and-signature-distinguished container", which is no longer named
by a name.

### What B costs, and why it is acceptable

```
  function use (x => a) { type token; … }
  function use (x => b) { type token; … }
      -> Shadowed. Rename one.
```

Rare, and the fix is one word. And there is a case that it is *right*: two
overloads of `use` are one operation over different inputs, so a local name
meaning two unrelated things across its variants is the same readability hazard H
refused inside a body — arriving between bodies that share a name.

**B also needs no new information.** The segment stays the shape words. The only
change is that H's uniqueness rule extends across all bodies of one name — the
same rule, one more extent, exactly as it already extends across anonymous
sub-scopes. A would have needed a new dependency; B needs a wider `Shadowed`.

## §1 — confirmed, and then ledgered, because a path is a location

Take the source path. It is strictly better than the empty string and it closes
the audit's probe.

But notice what it is: **a location** — and I have just ruled locations out as
identities. A path is far more stable than a span (files move rarely, lines move
constantly), so this is a difference of degree, not of kind. Renaming or moving a
file changes every type identity in it, which is arguably correct and rare.

So take it, **and put it in the ledger with its successor named**, because it is
the same shape as the span defect and the next reader should not have to rederive
that:

```
  approximation                 becomes
  module identity is the        the module's DECLARED NAME, if modules
  source path — a location,     acquire one; the path becomes incidental
  stable only while the file                and a file may then move freely
  does not move
```

If modules already declare a name, use that instead and the row is unnecessary.
That is a question about the tree, so it is yours.

## §3 — confirmed

The audit is right and the distinction is the one that matters: **equality hashes
a structure; the string is a rendering.** One addition worth building in — the
render function should be the *only* place the string form exists, and **nothing
should ever parse one back**. A path that can be parsed becomes a path that is
parsed, and then the presentation format is load-bearing.

## Finding 2, and one note on finding 4

**Landing finding 2 with the refactor is right** — same machinery, and patching
the current path twice is how a stopping condition ends up stated in two places
that later disagree.

On **finding 4**: the audit's objection is correct and worth accepting rather
than defending. `CHECKERSCOPINGRULINGS` Q1 asked for the case to be *shaped* so
later construction sites are not rewritten; an external map keyed by variable
identity is a different architecture, and — the audit's sharp point — *"any
hashable object could someday be an external key; that does not itself implement
the requested accommodation."*

Give `Variable` an explicit requirements handle now, empty, without the
constraint machinery behind it. The concrete reason: a `Variable` handed to
another pass with an external map carries **no way to ask what it requires** —
the caller must also be handed the map, and its lifetime and ownership are
unspecified. A slot makes the question answerable from the value.

## Summary

| | |
|---|---|
| **§2** | **B — the overload set is one container** |
| why not A | it makes identity depend on **resolved parameter sorts**, so editing a signature changes the identity of every type in its body. **A worse instability than the span had**, in a new place — and possibly circular under H-wide |
| B and the ruling | B is the **literal** reading of *nearest named container*; A redefines the term |
| B's cost | two same-named locals across overload bodies become `Shadowed`. Rare, one-word fix, and arguably the same hazard H already refuses |
| B's mechanism | **no new information** — the segment stays the shape words; H's uniqueness rule gains one more extent |
| **§1** | **confirmed** — but a path is a **location**, the category I just ruled out. Take it, **ledger it**, successor = a declared module name |
| **§3** | **confirmed.** Equality hashes a structure; the string is a rendering — and **nothing may parse one back** |
| finding 2 | land it with the refactor — right call |
| finding 4 | **accept the audit.** An explicit empty slot now; an external map leaves a `Variable` unable to answer what it requires |
