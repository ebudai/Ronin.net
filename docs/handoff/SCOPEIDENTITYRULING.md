# Scope identity — **H**, and it is not a compromise

> **Ledger** — `[V]` Answers `SCOPEIDENTITY.md`: **H, wide reading**. A type
> declaration belongs to its nearest **named** container (module, type body,
> function); identity is «(container path, name)»; a type name is unique within that
> container across its anonymous sub-scopes, so sibling-block same-name becomes
> «Shadowed». A `type X;` has no runtime lifetime, so block scoping was a lifetime
> notion misapplied. C stays rejected; finding 3 proceeds in parallel.
> supersedes: not yet checked
> superseded by: not yet checked

**Ruling: H**, in the wider of its two readings (§2). Build finding 3 in parallel
as you propose.

Your own critique of A is the thing that decides it: *"its anonymous ordinal is
asserted rather than unique-by-a-rule — and stability and unique-by-a-rule are the
two properties you chose (scope, name) **for**."* That is exactly right, and it is
the sentence I would have had to write. A is a correct answer that abandons both
reasons for the design it implements.

Nothing here is measured; there is nothing measurable in it that your probe has
not already established.

---

## 1. Why H rather than A — the readability argument, not the caching one

You priced A's weakness as a cache miss on a rare edit, and that pricing is fair.
But it is not the reason to refuse A. **The reason is what A permits in the
source:**

```ronin
  function f {
      { type token;  … }        two DISTINCT types
      { type token;  … }        one spelling, no cue
  }
```

One function, one word, two meanings, and nothing visible telling them apart. That
is the class of thing this language refuses everywhere else — it is `wait time`
and `hidden cost` in a third costume. A reader who meets `token` twice in one
function has no way to know they are not the same type, and no bracketing, hover
or annotation would help, because the two declarations are both perfectly ordinary.

H makes that source `Shadowed`. **One function, one `token`.** That is a
readability property bought with a refusal, in a language whose stated premise is
readability at the cost of writability.

## 2. Why H is principled rather than a compromise — take the wider reading

H has two readings and you should take the second:

- **narrow** — identity is `(container, name)`; visibility stays block-scoped. The
  type is unique across the function but nameable only in its block.
- **wide** — the type **belongs to** the nearest named container: unique there,
  and nameable there.

Narrow is a rule with two different extents, which is the shape that confuses
people. Wide is one extent, one rule — and there is a reason it is the right one:

> **`type X;` is not a statement in the executable sense.** It declares a
> compile-time entity; it has no runtime lifetime. Block scoping is a
> *lifetime* notion, and applying it to a thing with no lifetime is an artefact of
> the declaration being *parsed* as a statement rather than a fact about what it
> is.

So the wide reading is not "hoisting" as a convenience. It is recognising what a
type declaration already was. That is what makes H principled rather than a
trade — and it means the identity is `(nearest named container, name)`, stable
because the container is named, unique because the extended `Shadowed` rule says
so.

## 3. Why not E

E is the blanket form of H's exact condition, and I have been wrong in that
direction often enough to name it. The hazard is *two same-named types in one
function*; E's remedy is *no types in anonymous scopes at all*. That refuses a
construct to fix a naming rule, and it removes a capability — a type local to a
block — that nothing has shown to be harmful.

H refuses the collision; E refuses the location. The collision is the problem.

## 4. What this costs, and it should not be silent

**This is a language-semantics change, and you were right to bring it.** Source
that compiles today stops compiling:

```
  two same-named types in sibling blocks of one function
      today  -> two distinct types, clean
      under H -> Shadowed
```

Your evidence is that no fixture exercises it, so the practical cost is near zero
— but it goes in the spec as a stated rule, not as a silent tightening:

> **A type declaration belongs to its nearest named container — a module, a type
> body, or a function. A type name is unique within that container, across its
> anonymous sub-scopes.**

Two things to get right while building it:

**"Nearest named container" must be defined by what has a name, not by node type.**
A delegate body is anonymous, so a type declared there belongs to the enclosing
function. A `when` body likewise. Writing the list of *named* containers — module,
type body, function — is better than writing the list of transparent ones, because
the second list grows every time a construct is added and the first does not.

**Nested containers give a path**, and that is fine: `module → f → token` is a
value, stable under any edit that does not rename an enclosing container. Renaming
one *should* change the identity, because it changes what the type is called.

## 5. And C stays rejected, for the record you made

Your rejection of C is right and worth keeping where you put it: treating
anonymous scopes as transparent *without* the uniqueness rule reintroduces
finding 1 one level in. H is C **plus the rule**, and the rule is the entire
difference. Recording the near-miss so it is not re-proposed is the right habit —
it is the same job the expiry ledger does for approximations.

## 6. Summary

| | |
|---|---|
| ruling | **H, wide reading** — a type declaration belongs to its **nearest named container**, and is unique there |
| why not A | not the cache miss — **two same-named types in one function, no cue.** `wait time` in a third costume |
| your critique of A | decisive and correct: an ordinal is **asserted**, not unique-by-a-rule, and that was one of the two reasons for `(scope, name)` |
| why wide, not narrow | narrow is one rule with two extents. And **`type X;` has no runtime lifetime**, so block scoping was a lifetime notion misapplied — wide recognises what the declaration already is |
| why not E | it refuses the **location** to fix a **collision**. The blanket form of a narrow condition |
| the cost | a real semantics change; sibling-block same-name becomes `Shadowed`. **Put it in the spec as a rule**, not a silent tightening |
| define containers by | **what has a name** — module, type body, function. That list does not grow when a construct is added; the transparent list would |
| C | stays rejected, and keeping the record is the right habit |
| finding 3 | **yes, in parallel** |
