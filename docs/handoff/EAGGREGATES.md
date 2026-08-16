# E — square aggregates: resolution, evaluation, and runtime values

> **Ledger** — `[R]` E — square aggregates: resolution, evaluation, and runtime values
> supersedes: none
> superseded by: EAGGREGATES2

Written against `1b1f788` rather than against a description, which changes one
thing materially: **a lookup literal already parses, gets its duplicate-key
check, and then loses its keys.** So E is not "design lists and lookups". Lists
are done. E is one missing value type, one missing slot in the resolution node,
and four decisions that follow.

---

## 0. What the tree says

**Built, and E builds on it rather than around it.**

`Grammar/Collection.cs` — one production and one decision. A `[…]` is parsed
once; the kind is decided from *every* element, so a mixed collection is one
message naming both positions. Duplicate keys are refused at parse time by
length-prefixed token identity. That is finding 8 and half of D, done.

`Runtime/List.cs` — a sealed value: private storage, immutable, compared by
content. `Admit` is *the* boundary, deep, idempotent on non-arrays. Depth is
carried on the value, refused past `Deep = 256` **at construction** rather than
capped at comparison, because "a value the runtime accepts must be one it can
compare honestly". Cycles are refused with a message that can name the argument.
A second reference to one array is reused, so a DAG stays a DAG instead of
exploding into a tree.

That file has already made most of E's hard calls. E should inherit them, not
re-decide them.

**Missing, and this is the shape of the work.**

```
  Runtime/Lookup.cs                does not exist
  grep -r "Association|Origin" Compiler/Resolution Compiler/Runtime
                                   -> nothing
  Resolution/Node.cs               Group carries «Collection: bool» + flat Parts
  Runtime/Evaluator.cs:113         a collection group -> List.Admit(...)
```

So `[a = 1, b = 2]` parses as a lookup, is checked for duplicates as a lookup,
and then reaches the runtime as **a list of two values with the keys thrown
away.** Nothing downstream can be wrong about it, because nothing downstream
knows.

## 1. Resolution: three kinds, not a boolean, and keys on the node

`Node.Group(bool Collection, Parts)` has to grow a key slot. Two ways, and the
choice is decided by this project's own history rather than by taste.

A parallel `Keys` array is the cheap version and it is the version that breaks:
`Group.Same` and `Group.Hash` would each need to remember it, and *a fact kept in
a second place, surviving the change that falsifies it* is the failure this
codebase has now hit five times. Two lookups differing only in their keys would
compare `Same`, and derivation identity would collapse them.

> **One node type carries both.** A collection's parts are `Entry(Key, Value)`
> with `Key` null for a list, so `Same` and `Hash` cannot be written without
> reaching the key.

And the boolean should become a **kind with three values** — `Group`, `List`,
`Lookup` — rather than a bool plus a nullable-key convention. Three states
encoded in two independent fields is two fields that can disagree; the parse
already decides which of the three it is, and it decides from every element.

That also gives `Repair` and `Completion` something to say: a synthetic bracket
inside a lookup is a different suggestion from one inside a list, and today they
cannot tell.

## 2. The runtime lookup value, mirroring `List`

Sealed, for the reason `List.cs` already gives — *a list and a lookup need
different equalities, so they need different types for equality to dispatch on,
and `x is a lookup` needs a type to answer from.* A tag beside the value is a tag
that can be wrong.

Private storage, immutable, compared by content, with entries carried in
insertion order (see §6). And it goes through **the same `Admit`**, not a second
one — because `Admit`'s stated point is that *"an API cannot be wrong about
whether it needs it"*, and two admission functions puts that choice back at every
call site. One traversal, one `inside` set, one `done` map, both kinds.

Which means the raw pre-admission carriers must be distinguishable by CLR type
the way `object[]` is today — a list arrives as `object[]`, a lookup as a
distinct pair-carrier. Same argument as the sealed value type, one stage earlier.

## 3. One depth measure across both kinds — measured

This is the first thing that goes wrong if the lookup is built beside the list
rather than with it.

```
   layers  shared  list ctr  lookup ctr    shared says   per-kind says
        4       4         2           2          admit           admit
        9       9         5           4         REFUSE           admit   <- BYPASS
       16      16         8           8         REFUSE           admit   <- BYPASS
       17      17         9           8         REFUSE          REFUSE
   disagreements: 8      (limit 8, scaled down from 256 to be printable)
```

A value that alternates list and lookup is **16 layers deep with neither counter
past 8.** Alternating halves every counter, so a per-kind limit is really the
limit times the number of kinds — and a third aggregate would raise it again.

That matters for the reason `List.cs` states: the limit exists so the value can
be *compared*, and cutoff, `changes`, `old` and `is` all ask. A value admitted
at twice the depth is one the comparison was sized to refuse.

> **One depth measure across every aggregate kind, carried on the value — and a
> key counts toward it exactly as an element does.**

`Fits` already re-asks on reuse because depth is a property of the whole value.
This is the same argument one kind further out.

## 4. Keys canonicalised at construction — the runtime half of D

`Collection.Element`'s own comment gets this exactly right and stops one step
short of the consequence: the parser answers the **spelled** duplicate; *"two
keys that differ in spelling and agree in value are the runtime's question and
arrive with the lookup value."*

Measured, with a lookup used as a key:

```
  [ x = 1, y = 2 ]  and  [ y = 2, x = 1 ]   as keys of an outer lookup
      canonical form equal : True
      outer lookups        : EQUAL
      the outer lookup has : ONE key
```

Two distinct token spellings, one key. The parser admits both; the runtime must
not.

> **A lookup canonicalises its keys at construction, and two entries whose keys
> canonicalise alike are refused there** — the same shape as `TooDeep`, becoming
> an `Error` at the boundary, for the same reason.

And the cheaper thing someone will reach for instead — comparing keys
structurally with a budget — is already refused for lists and inherits the
refusal:

```
  two values differing only below the budget
      capped comparison : EQUAL -- WRONG
      canonical forms   : unequal -- correct
```

A cap makes two unequal values compare equal, which is not an equivalence, and
it is observable through cutoff and `old`.

## 5. `[]`

The parse gives an empty `Collection` with `associated == 0`, so today it is a
list, and that is the right default: **`[]` is the empty list.** `List.Empty` is
already a singleton for it.

The empty lookup then has no literal of its own, and should not get one — a
second spelling for "empty" is a spelling people will get wrong. It comes from
the expected type:

```
  var xs                          = []      list of ?
  var xs => list of number        = []      list of number
  var m  => lookup of text number = []      the empty LOOKUP
```

This is the one place outward-in typing (`GENERICS-II` §4) is load-bearing for
aggregates, and it is the same shape as `return empty list` in
`RECURSIVE-RETURN` §1 — an under-determined literal pinned by its context. Which
is a check on that ruling rather than a new mechanism.

`Lookup.Empty` is a singleton beside `List.Empty`, and the two are **not equal**,
because they have different types and a comparison between them is a type error
rather than a false. That is the no-subtyping payoff arriving early.

## 6. Iteration order

A lookup is unordered *for equality* — that is what makes §4's canonical form a
sorted one. It cannot be unordered *for iteration*: in an always-running IDE
where debug is development, a `for each` whose order varies between runs makes a
program irreproducible, and the bug it hides is the worst kind.

> **Insertion order is preserved for iteration and ignored for equality.**

Two lookups may therefore be equal and iterate differently. That is the correct
trade and it should be said out loud in the reference entry, because it is the
one surprising thing about the type.

## 7. Typing

No subtyping, so no joins, so this is short:

```
  [ e1, … en ]     unify every element type      -> list of T
                   elements disagree             -> error, naming both positions
                                                    (the same message shape
                                                     Mismatched already uses)
  [ k1 = v1, … ]   unify keys -> K, values -> V  -> lookup of K V
  [ ]              -> list of ?, a type variable
```

**Unify, not check** — the element that pins `T` may be the third one, exactly as
the recursive site is what pins `list of ?` in `RECURSIVE-RETURN` §1. Taking the
first element's type as the answer and validating the rest against it is the same
mistake in a second place.

## 8. Indexing, and what a miss gives

```
  xs [ i ]     list of T,   i => number   -> T
  m  [ k ]     lookup of K V, k => K      -> V
```

The interesting half is the miss, and **it must be an `Error`, not `nothing`** —
for a reason that does not depend on the `Error`/`optional` fork being settled:

> If a miss gives `nothing`, then `lookup of K (optional V)` cannot distinguish
> **absent** from **present, and the value is nothing.** The type is not exotic;
> it is what you get the first time anyone stores an optional.

`NOTHING-AND-INDEXING` §1.1 already rules `x + 1` on `nothing` an `Error` rather
than a propagating nothing, on the same reasoning — a missing value must not
quietly become a wrong answer — and `otherwise` catches it. So this is consistent
rather than new.

A list index out of range is the same: `Error`, with the length in the message.

## 9. One invariant that needs a consumer

`Collection.cs` names it and it is now load-bearing for the single-parse
property:

> **«=» inside brackets is only ever an association separator, never an
> expression operator.** *"If that ever stops being true, this stops being a
> decision and becomes a guess, and the exponential returns through a door nobody
> is watching."*

That is a fact living in a comment, which is the one place with no consumer — the
thing we just paid for with the stale megabyte figure. **Give it one: a test
asserting `=` is absent from the operator table**, failing with that sentence.
The ladder work (`STOP-AND-LADDER` §3) is about to make the operator table
user-extensible, which is precisely when the door opens.

## 10. What this unblocks, and what it still waits on

**`match`** becomes writable: an arm list is a collection of associations, so
`match` is a lookup literal whose keys are patterns. Worth noting the shape here
and designing it separately — the pattern-as-key question is its own document.

**Still forked:** `Error` and `optional`, which I raised before starting. It does
not block §§1–9 — every decision above holds either way, which is a sign they are
the right ones — but it decides whether `lookup of K V` indexing can be given a
total type, and it decides the checker's whole shape. That is the one thing I
still want from Budai before the type checker.

## 11. Summary

| | |
|---|---|
| lists | **done** — E inherits `List.cs`'s calls rather than re-deciding them |
| the actual gap | a lookup literal **parses, is duplicate-checked, then loses its keys** — nothing downstream knows |
| resolution node | **three kinds, not a bool**, and parts are `Entry(Key, Value)` so `Same`/`Hash` cannot omit the key |
| the lookup value | sealed, private storage, insertion-ordered, through **the same `Admit`** — one traversal, both kinds |
| depth | **one measure across kinds**, keys count. Measured: per-kind counters admit 16 layers under a limit of 8 |
| duplicate keys | parser answers **spelled**, runtime answers **value** — canonicalise at construction and refuse there |
| a capped key comparison | refused — it makes unequal values compare equal, observable through cutoff and `old` |
| `[]` | the **empty list**. The empty lookup comes from the expected type; no second literal |
| iteration | **insertion order for iteration, ignored for equality** — reproducibility beats purity here |
| typing | unify elements, **not** check against the first. `[]` is `list of ?` |
| a miss | **`Error`, never `nothing`** — otherwise `lookup of K (optional V)` cannot tell absent from present-and-nothing |
| the `=` invariant | **needs a test**, not a comment. The ladder is about to open that door |

Probe: `aggregate_depth.py`.
