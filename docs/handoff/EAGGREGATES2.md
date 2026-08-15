# E — square aggregates: resolution, evaluation, and runtime values

> **Ledger** — `[R]` recommendation on lists and lookups. §8 (the miss) superseded
> by `REAUDIT47RULING` §5; §10 (the `Error`/`optional` fork) closed and struck below,
> per `REAUDIT47RULING` §5, `ERRORASVALUE`, and `CHECKERSCOPINGRULINGS` Q2. §1's
> "a lookup does not resolve at all" is stale — the three-kind node has since landed.

**Revised.** The first version of §0 stated a premise that is not what the code
does, the programmer verified it rather than building on it, and §1 is rewritten
around what he found. The correction is in §0 and the reasoning error is named
there, because it is the more useful half.

Written against `1b1f788`. Lists are done and E should inherit their decisions
rather than re-take them. What E is: **teaching the resolver to resolve a lookup
literal at all**, then a missing runtime value type, then the decisions that
follow from both.

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
  Resolver.Group                   splits on Separator at depth 0, and only that
```

### CORRECTION — the first version of this section was wrong

It said `[a = 1, b = 2]` *"reaches the runtime as a list of two values with the
keys thrown away."* **It does not reach the runtime at all.**

`Resolver.Group` collects `LexemeKind.Separator` at depth 0 and splits on those.
`=` lexes to `LexemeKind.Symbol`, nothing combines a key with a value, so the
part `a = 1` has no derivation, `part.IsEmpty` fires, and the whole group is
refused. Probed: `[ a = 1 ]` → NoParse, `[ 1 = 2 ]` → NoParse, while `[ 1, 2 ]`
and `[ a, b ]` resolve.

How the error was made, because the shape matters more than the fact: I observed
that keys have no consumer downstream, and that the evaluator turns a collection
group into `List.Admit`. From those two endpoints I asserted a **path** I never
observed. The absence of key handling downstream is equally consistent with *keys
are dropped* and *lookups never arrive* — and I picked one and wrote it as what
the code does. Same class as the stale megabyte figure: a conclusion assembled
from two ends with the middle unchecked.

**So the keys are not the gap. The resolution is**, and §1 below is rewritten
accordingly. Everything from §2 onward is unaffected — it was about values and
types, not about how the literal gets there.

## 1. Resolution: teach the resolver to resolve a lookup

This is the real first step, and it is upstream of everything else.

### 1a. `=` gets its own lexeme kind

Today the resolver would have to ask `lexemes[k].Text is "="`, which is how
`collection: lexemes[i].Text is "["` already works — so there is precedent, and
it is precedent for the wrong thing. A load-bearing structural decision made by
string comparison is a decision anything can break silently.

> **Add `LexemeKind.Associates`, beside `Separator`.**

And this is what §9's invariant has been waiting for. *"«=» inside brackets is
only ever an association separator, never an expression operator"* stops being a
comment and becomes a **kind** — a fact with a consumer, enforced by the type
rather than by memory. If someone later adds `=` to the operator table, the
lexeme kind is what refuses it.

*A question I cannot settle from the tree:* the resolver is handed the tokens of
one statement, and `var x = 3`'s `=` is consumed by the grammar before the value
span is taken — so every `=` the resolver sees should already be inside brackets.
If any path hands the resolver a span containing a statement-level `=`, the kind
has to be assigned with bracket context rather than lexically. Worth one probe
before relying on it.

### 1b. `Group` splits twice, and the second split is subordinate

The comma split stays exactly as it is. When `collection` is true, each part is
then split at its depth-0 `Associates`:

```
  0 in the part    the part is a VALUE          -> a list entry
  1 in the part    key span | value span,       -> Entry(Key, Value)
                   resolved independently
  2 or more        a finding, not an alternative
                   «this entry has two «=»; an association has one»
```

**One decision, no speculation** — which is the same discipline as the comma
split, and it is what keeps §9's single-parse property. A key that is itself a
collection has its `=` at depth > 0, so "the first at depth 0" is not a heuristic;
it is exact, and it is exact *because* of the invariant.

### 1c. Which check owns "mixed"

`Collection.Parse` already refuses a part-list part-lookup collection with a good
message. After 1b the resolver can derive the same thing, and two derivations of
one rule is the failure class this project keeps paying for.

The resolver can be driven directly, without the grammar — that is how the
NoParse above was probed — so it cannot simply assert the grammar got there
first. So: **one predicate, two callers.** `Collection.Parse`'s check becomes a
call into the same *does this entry have a key* predicate the resolver splits on,
and the message stays where it is. Same move as the descriptor slice.

### 1d. Then the node, which is what the first version of this section said

Only now does the key slot matter, and the shape stands unchanged:

> **One node type carries both.** A collection's parts are `Entry(Key, Value)`
> with `Key` null for a list, so `Same` and `Hash` cannot be written without
> reaching the key.

A parallel `Keys` array is the cheap version and it is the version that breaks:
`Group.Same` and `Group.Hash` would each have to remember it, and two lookups
differing only in their keys would compare `Same`, collapsing derivation
identity.

And the boolean should become a **kind with three values** — `Group`, `List`,
`Lookup` — rather than a bool plus a nullable-key convention. Three states in two
independent fields is two fields that can disagree.

That also gives `Repair` and `Completion` something to say: a synthetic bracket
inside a lookup is a different suggestion from one inside a list.

### 1e. One thing to measure rather than assume

`Combinations` currently takes the product over *n* parts. With keys it is over
up to *2n* cells, so a lookup's derivation count is the product of its keys' and
values' counts. `Cell.Saturating` and `Beyond` already exist for exactly this, so
the machinery is there — but the *rate* is new, and it is the kind of number I
have twice now been wrong to predict. **Measure it on a nested lookup before
trusting it**, and if it bites, the answer is likely that a key is a much smaller
grammar than a value rather than that the product needs bounding.

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

> **SUPERSEDED — see `REAUDIT47RULING.md` §5, which replaces this section in
> full.** A lookup miss gives `nothing`, not an `Error`, and `m @ k` is typed
> `optional V`. The argument below types `m @ k` as `V` and then reasons from
> there that a miss must be an `Error`; both halves are withdrawn. A list index
> out of range remains an `Error`. The rest of this document is unaffected.

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

**~~Still forked:~~ SUPERSEDED — the fork is closed.** The miss is ruled in
`REAUDIT47RULING` §5 (`nothing`, `m @ k : optional V`, optionals nest, list index
out of range stays `Error`), and the fork behind it in `ERRORASVALUE` (a named,
one-directional bottom; no union). `CHECKERSCOPINGRULINGS` Q2 confirms both: the
checker is equality-unification with an `Error` bottom and `optional` nesting, and
nothing here remains open. The struck sentence — *"the one thing I still want from
Budai before the type checker"* — asked for a decision already made, and cost a
reader the half hour that motivated this note.

## 11. Summary

| | |
|---|---|
| lists | **done** — E inherits `List.cs`'s calls rather than re-deciding them |
| the actual gap | **a lookup literal does not resolve at all** — `Resolver.Group` splits only on `Separator`, so `a = 1` has no derivation. My first §0 said the keys were dropped; they never arrive |
| the real first step | `LexemeKind.Associates`, a **second split subordinate to the comma split**, one predicate for "mixed" with two callers |
| then the node | **three kinds, not a bool**, parts are `Entry(Key, Value)` so `Same`/`Hash` cannot omit the key |
| the lookup value | sealed, private storage, insertion-ordered, through **the same `Admit`** — one traversal, both kinds |
| depth | **one measure across kinds**, keys count. Measured: per-kind counters admit 16 layers under a limit of 8 |
| duplicate keys | parser answers **spelled**, runtime answers **value** — canonicalise at construction and refuse there |
| a capped key comparison | refused — it makes unequal values compare equal, observable through cutoff and `old` |
| `[]` | the **empty list**. The empty lookup comes from the expected type; no second literal |
| iteration | **insertion order for iteration, ignored for equality** — reproducibility beats purity here |
| typing | unify elements, **not** check against the first. `[]` is `list of ?` |
| a miss | **`Error`, never `nothing`** — otherwise `lookup of K (optional V)` cannot tell absent from present-and-nothing |
| the `=` invariant | **becomes a lexeme kind**, which is better than the test I asked for — enforced by the type rather than remembered |

Probe: `aggregate_depth.py`.
