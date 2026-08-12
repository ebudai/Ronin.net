# Generics, second pass — the cheap route works, and one addition makes it safe

Taking the three proposals in turn: no erasure, type parameters always
compile-time, and *omit the type and it is generic*. I agree with all three.
The third has one defect, it is the same defect that ruled out Hindley-Milner,
and it can be fixed with **no new syntax at all** — which is worth doing now
because the fix constrains what the compiler records, not what the programmer
writes.

---

## 1. Erasure — right instinct, and worth correcting the reason

The superstition is close to a real thing but not the thing. Erasure does not
deprive the *compiler* of information; Java's compiler knows every type
precisely. What it deprives is the **runtime layout**: an erased `list of
number` is an array of pointers to boxed values, so you pay an indirection per
element, you lose contiguity, and nothing vectorises.

So the accurate statement is *erasure costs layout, not knowledge* — and that
is exactly why it is unavailable here. `INSTANCEBINDING.md` settled one cell
per member holding N values. An array needs a concrete element type to be an
array. Monomorphisation is **forced**, not chosen, and your preference costs
nothing because there was no alternative to give up.

Its real prices are code size per instantiation and compile time. One note for
the programmer: in an always-running environment, a call at a *new* argument
type triggers an instantiation **mid-session**, so instantiation has to be
incremental and cheap rather than a whole-program pass.

## 2. Type parameters always compile-time — agreed, and name the consequence

Agreed, and it is the rule that makes everything else simple. But it has a
consequence someone will hit in week one and it should be written down rather
than discovered:

**There is no `any`, and there cannot be a heterogeneous container.** If a type
parameter is compile-time, `list of (_)` cannot have its element type decided
at runtime. VB6 programmers reached for `Variant` constantly, and this removes
it.

The replacement is already in the language and is better: **a sum type**.
`list of (number or text)` is a closed set, monomorphisable, and — because
`MATCH.md` made exhaustiveness fall out of `optional` — reading one *forces*
you to handle both cases. That is the `Variant` use case, checked. It should be
in the docs beside the rule, because "where did Variant go" is the first
question the target audience asks.

## 3. Omit the type and it is generic — agreed on the spelling

It costs no new syntax, which is the best possible property. A typed parameter
is already `(items => list of number)`; the untyped one is `(items)`. Nothing
to invent, and it reads correctly: *this works on whatever fits*.

Worth naming what it is, so we inherit the known consequences deliberately:
this is **Zig's `anytype` and C++'s template parameter**. Structural, implicit,
monomorphised, constraints never written down.

And that family has exactly one famous defect.

## 4. The defect: a generic function has no type until it is instantiated

Which means a **caller's** mistake is reported inside the **callee**.

This is the failure `GENERICS.md` §4 used to rule out Hindley-Milner — "error
messages arbitrarily far from the mistake" — arriving by a different road. It
would be a poor outcome to reject global inference on that ground and then
adopt templates, which have the same property in a more localised form.

`constraint_infer.py` builds both modes so the difference is concrete:

```
  a list of truth into a summing function:  total of list of truth

    -- today: instantiate, then fail inside the body
    total of.ron:13: no «(_) + (_)» for «truth» and «truth»
      (instantiated from caller.ron:47 — the caller wrote no «truth» here
       and cannot see this line)
```

Ronin's version is much milder than C++'s — no overload sets, no SFINAE, no
two-phase lookup, so the message is one line rather than a wall. But it is
still a message about a line the programmer did not write, in a function they
may not own.

## 5. The fix, and it needs no syntax

The compiler already walks the body to monomorphise it. **While walking, record
what the body requires of each parameter** — the set of pattern resolutions
that must succeed. Then check that set at the call site, before entering the
body.

```
  function total of (items)
    inferred interface:
      «count of (_)» over «items»                        (line 11)
      «(_) @ (_)» over «items», number                   (line 12)
      «(_) + (_)» over result of line 12, twice          (line 13)
```

Same program, same error, different place:

```
    caller.ron:47: «total of» cannot take «list of truth»
      it requires «(_) + (_)» for «truth» and «truth», which does not resolve
      (required at total of.ron:13)
```

Two locations, both real: where the mistake was made, and why. The programmer
writes nothing.

This is a C++20 concept, derived rather than declared — and in Ronin it is
unusually natural, because a requirement is literally *"this pattern must
resolve for these types"*, which is the resolver's existing question asked with
types instead of names.

### Sameness falls out, and more precisely than a declaration would

`max of (a) (b)` never says the two are the same type. It does not need to:
the body uses `(_) > (_)` on the pair, so the requirement is recorded **over
the pair**.

```
    max of number number    OK
    max of text text        OK
    max of number text      REJECTED at the call site
```

A declared `T, T` would say this less precisely — it would also reject
`number` against `big number`, where `(_) > (_)` is perfectly well defined.
The inferred version is both cheaper to write and more permissive where
permissiveness is correct.

## 6. Why this matters more than a diagnostic — the module interface

This is the argument that moves it from *nice* to *decide now*, and it lands on
the seam `FAILUREMODES.md` §1 just made load-bearing.

**A function with no declared parameter types has no exportable interface.**
Its published signature is "takes anything." So a module can export `total of
(_)`, an importer can call it wrongly, and the error appears inside a module
the importer cannot edit — which is precisely the situation compiled-scope
resolution was adopted to prevent, reintroduced through the type system.

The inferred requirement set **is** the interface. It is exportable, it is
checkable at the boundary without the body, and it makes a generic export as
honest as a typed one. Without it, generics quietly punch a hole in the module
guarantee.

### Two honest costs

**Chains.** If `total of` calls `helper of` calls `deeper of`, the requirement
propagates and the message becomes a chain — *required at deeper.ron:3, via
helper.ron:9, from total of.ron:13*. Better than C++'s wall, not free. Report
the chain, capped at a few frames.

**Editing a body changes the interface.** Adding `upper (_)` to `total of`
narrows the types it accepts and breaks importers, with no signature change to
review. That is the true price of not declaring constraints, and it is a real
versioning hazard.

But the mitigation is a feature rather than a patch, and the always-running
environment is what makes it possible: **show the inferred interface, and flag
when an edit narrows it.** §2 of the probe prints exactly what that display
looks like. "This edit removes `list of truth` from what `total of` accepts"
is a better review artefact than a signature, because it is derived from the
code rather than asserted alongside it.

## 7. `type of x` at compile time — yes, with one thing held back

Exposing it is right and cheap. What I would hold back is **branching on it
inside a generic body**:

```ronin
function describe of (x) => match type of x [number = …, text = …] otherwise …
```

That is Zig's `comptime`, and it is genuinely powerful. It is also ad-hoc
polymorphism that is **invisible in the signature** — a function whose
behaviour depends on types in a way no reader can see without opening it. For a
language whose premise is that the obvious reading is the true one, that is the
wrong default, and it interacts badly with §6: a branch means the inferred
interface becomes a *disjunction* rather than a set, and the useful error
message degrades.

`MATCH.md` §6a already applies here — `type of x` is an open universe, never
exhaustive, so it is always `optional T` and always needs `otherwise`.

Recommendation: **expose `type of x`; defer branching on it inside generic
bodies until a real use case turns up.** Deferring costs nothing; removing it
later costs programs.

## 8. Two decisions this forces that are not yet made

**a. Are types and values one symbol table or two?**

They must be decided together with R5/R6b, because those rules are properties
of a table. If shared, a stdlib function pattern `count of (_)` collides with a
type constructor `count of (_)`, and every type name competes with every value
name for the same prefix space. If separate, the rules run twice over smaller
tables and **glue words are spent per namespace** — which roughly halves the
registry pressure we have been fighting.

I would separate them. The syntactic positions are already distinct (a type
follows `=>`), the resolver is unchanged — same machinery, different table
selected by position — and "one resolver, two languages" survives intact.

**b. R6b applies to type constructors too.**

`stack of (_)` declared by a user is anchor-only, so no *type* name may begin
`stack of`. Cheap and correct, but the registry generator has to cover the type
namespace or the check silently does not run there.

## 9. What still stands from the first pass, and what changes

| | |
|---|---|
| type constructors as patterns | unchanged — measured, zero glue, nests |
| monomorphisation | unchanged — **forced**, and your preference costs nothing |
| variance follows `var`/`let` | unchanged, and unaffected by all of the above |
| generic declarations deferred | **withdrawn** — your route removes the problem I could not solve. There is no type-variable spelling to invent because there are no type variables |
| constraints deferred | **partly withdrawn** — *declared* constraints stay deferred; *inferred* ones should be built with the monomorphiser, because retrofitting them means changing what a module exports |
| no `any` | new — say it out loud, and point at sum types |
| type/value namespaces | new — decide before R5/R6b are extended to types |

## 10. What I would send the programmer

1. Untyped parameter = generic; type parameters compile-time everywhere.
2. The monomorphiser records the requirement set per parameter and **checks it
   at the call site**; the requirement set is the exported interface for a
   generic.
3. `type of x` available at compile time; **no branching on it** in generic
   bodies for now.
4. Types and values get **separate symbol tables**; R5/R6b and the registry run
   over both.
5. Doc note: there is no `any`; heterogeneity is a sum type.

Probe: `constraint_infer.py` — both diagnostic modes, the inferred interfaces,
and the sameness cases, runnable.
