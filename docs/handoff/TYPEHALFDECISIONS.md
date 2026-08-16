# The type half — four things to rule before it lands

> **Ledger** — `[R]` The type half — four things to rule before it lands
> answered by: TYPEHALFRULINGS
> supersedes: none
> superseded by: none

**From:** the successor, mid-step-4.
**State:** branch `resolver-and-symbol-separation` at `a41bde4`, working tree clean,
1,226 tests green. Nothing below is written yet — this is the probe before the
build, and each of the four changes what the code becomes.

`TYPECHECKERHANDOFF.md` §2 is the task: *annotations resolve with an expected
kind, and unknown or ambiguous type annotations become findings at the
annotation.* I have read the machinery end to end and it is ready to build. But
four decisions sit inside it that are yours and not mine, and the handoff's own
rule — *relay design questions, do not pick* — is why this exists rather than a
commit that guessed.

---

## 0. What the build is, so the decisions have a frame

The shape is settled by `FIVE-RULINGS` §4, `RETURN-AND-LITERALS` §4b and
`TYPE-VOCABULARY` §2, and I am not reopening any of it:

- **One table, kind is a filter.** `SymbolTable` already filters `Known` and
  `Callable` to `SymbolKind.Value`. The type side is the same read the other way:
  a `TypeNames` projection (`number`, `text`, `truth`, `error`, plus every
  declared `type X`) and a `TypePatterns` projection (`list of (_)`,
  `optional (_)`, `lookup (_) => (_)`, plus any declared type pattern).
- **`Compilation` closes the comment it already carries.** `Read` skips
  `Grammar.Type` today and says why: *"types resolve against a table that does not
  exist yet."* The table exists now. A parallel walk collects each
  `Type.Unresolved` annotation — on data, parameters and return types — resolves
  it through the **existing resolver** in type mode, and reports at the site.
- **Two findings.** A no-reading annotation is an **unknown type** (a new
  finding); an annotation with several type readings is an **ambiguity** (reuses
  `Ambiguous`, with the bracket repairs the resolver already produces). Unlike the
  value side, no-reading is *not* deferred here — `TYPE-VOCABULARY` §3 ruled that
  an unknown type name has no dependency to wait on and is reported where it was
  written, once, not once per use.

The blast radius is the sweep the handoff sized: fixtures using `money`,
`whole number`, bare `list`, and user types (`Dog`/`Car`) as annotations. It is a
separate commit and not what this memo is about.

The four questions are below, each self-contained, each with a recommendation and
what it costs.

---

## 1. `fast number` — a modifier, or a seventh prelude type name?

This is the one the handoff already flagged (§3) as yours: *"Not the programmer's
call."* It changes what the table holds, so it has to be answered before the
prelude is final.

**The stakes.** The ruling was that all numbers are `number` and context decides
the representation, with `fast number` the single exception (for `/fp:fast` on one
variable). Two ways to spell that:

| | costs | the table then holds |
|---|---|---|
| **`fast` as a modifier on `number`** | one reserved word (`Modifiers` is a fixed keyword set) | ONE number type, with a representation hint attached. The checker never sees two number types |
| **a seventh prelude type name** | no reserved word — `fast number` and `number` share the word `number` | TWO number types, told apart by name |

**Recommendation: the modifier.** The semantics as stated — one number type,
representation chosen by context — *are* the modifier: it keeps the checker's
world at one number type, so nothing downstream ever has to unify `fast number`
with `number` or decide which wins. The seventh-type spelling saves a reserved
word but pays for it by putting a second number type in front of every later pass
that reasons about numbers, which is the more expensive place to spend. And since
`Modifiers` is a fixed set either way, the reserved word is spent regardless of
which spelling you pick — so the "no reserved word" advantage of the type-name
spelling is smaller than it looks.

**Not blocking.** I can build the machinery for the six current prelude entries
now and add `fast number` in whichever form you rule. I need the answer to call
step 4 finished, not to start it. If you would rather defer it entirely, say so
and I will leave it out; any fixture using `fast number` reports unknown-type
until it lands.

---

## 2. Which positions does "annotation" cover — and do type-definition bases count?

An annotation reference (`Type.Unresolved`, carrying a `Reference`) sits in three
places, and I will resolve all three:

- `var x => T;` and `let`/`constant` data
- a parameter's type: `function f (x => T)`, `(x => T) => { … }`
- a return type: `function f (…) => T { … }`

There is a fourth run of type-words that is *not* an annotation: the **base of a
type definition**.

```
  type Car = Vehicle and { … }
             ^^^^^^^  an Algebra.Unresolved, a type reference too
```

`Algebra.Unresolved` also carries a `Reference`, so the same resolver would read
`Vehicle` and report it unknown if it is not declared. The question is whether
that belongs in *this* step.

**Recommendation: annotations only, and flag the base.** Three reasons:

1. **Every sweep fixture is an annotation.** `money`, `whole number`, `Dog`,
   `Car` are all `=> T` uses. Nothing in the sized list is a definition base.
2. **The base path is a different, riskier parse.** An algebra carries bases,
   unions, `and`/`or`, and a heading that a definition body follows. Resolving a
   union of type references is not the same shape as resolving one annotation, and
   folding it in doubles the surface of a commit whose value is in being small and
   auditable.
3. **It composes cleanly later.** The `Algebra.Unresolved` reference is reached by
   the identical walk with one more node type admitted; nothing about doing
   annotations first makes bases harder afterwards.

If you want bases in now, they are not hard — I want the boundary drawn on
purpose rather than by where I happened to stop.

---

## 3. How far does "kind = type only" reach — and where does the checker begin?

`RETURN-AND-LITERALS` §4b: *"a type position admits kind=type only."* The type
resolver runs the same DP as the value resolver, and the value resolver is
permissive on purpose — a value expression can be a literal, an operation, a
`old (_)`, a list. In type position those are all the wrong kind. If I leave them
active, they resolve **silently**:

```
  var x => list of 3           list of ⟨literal 3⟩         resolves, no finding
  var x => number + text       ⟨number⟩ + ⟨text⟩           resolves, no finding
  var x => old count           ⟨old count⟩                 resolves, no finding
  var x => list of [ 3 ]       list of ⟨[3]⟩               resolves, no finding
```

Each is a value reading standing in for a type, and a silent accept of a
non-type is exactly the "capture that looks like a feature" this project spends
its whole resolver design to prevent.

**Recommendation: the kind filter suppresses value-only readings, so those four
become unknown-type findings.** Concretely, in type mode the resolver offers no
number literal, admits no operator (the operator table is empty for a type
resolve), offers no `old (_)`, and offers no `[ … ]` collection — because a
literal, an operation, a previous-value and a list are each *a value*, which is
what "kind = type only" means read literally. This is not an extension of scope;
it is the kind predicate doing its job. `var x => 3` is already a parse error (a
lone anonymous value is not a reference), so this closes the cases that reach the
resolver, not a new class of them.

**Where I propose to stop, and want your line.** Two shapes are kind-*correct*
but arity- or multiplicity-wrong, and telling them apart is type **checking**, not
resolution:

```
  var x => list of (number, text)     a two-tuple filling a one-type hole
  var x => optional (a = b)           a keyed group filling a hole
```

`(number, text)` is a grouping of two types — both the right kind — and whether
`list of (_)` accepts a two-tuple is a question about the constructor's arity,
which is a later phase with a type to check against. I propose to **leave these to
that phase** and not reject them in the resolver, so a `( … )` grouping stays
available (it is how a bracketed hole is written: `list of (number)`), and only
the *value-kinded* readings above are suppressed now.

The line, stated once: **suppress what is inherently a value (literal, operator,
`old`, `[ … ]` collection); admit grouping; defer arity and multiplicity to the
checker.** If you would rather the resolver also refuse a multi-entry or keyed
group in type position now — cheap to do — say so.

---

## 4. The function/delegate type `(_) => (_)` is not in the prelude — and `LOOKUP-ARROW` §2 needs it

This is the one I could not have known to ask without reading `LOOKUP-ARROW-RULED`
against the actual `Supplies` list, and it is the most important line here.

The prelude supplies three type constructors:

```
  optional (_)          list of (_)          lookup (_) => (_)
```

There is **no standalone function type** `(_) => (_)`. But `LOOKUP-ARROW-RULED`
§1 rests on one existing:

```
  m => lookup text => number            1 reading   ascribe(m : LOOKUP[text -> number])
  m => text => number                   1 reading   ascribe(m : DELEGATE(text -> number))   <-- needs (_) => (_)
  m => lookup text => number => truth   3 readings  AMBIGUOUS                                <-- needs (_) => (_)
```

With only `lookup (_) => (_)` in the vocabulary, I have traced what actually
resolves:

- **`var m => lookup text => number`** → **one reading.** ✓ Exactly §1's
  common-shape claim, and its argument holds: the two arrows do not tie, because
  the kind filter admits only the reading where each arrow's operands are types.
  This *is* `FIVE-RULINGS` §4's third load-bearing appearance, and it works today.
- **`var m => text => number`** → **no reading → unknown type.** `text => number`
  is a *function* type, and the prelude names no such constructor, so nothing
  reads it.
- **`var m => lookup text => number => truth`** → **no reading → unknown type**,
  *not* the 3-way ambiguity §2 requires. All three of §2's readings
  (`lookup text => (number => truth)`, `(lookup text => number) => truth`,
  `lookup (text => number) => truth`) need `number => truth` / `text => number`
  to be types, i.e. they need `(_) => (_)`. Without it there is nothing to be
  ambiguous *between*.

So `LOOKUP-ARROW-RULED` §2, which the handoff (§5) lists as *"a maintained-test
target once annotations resolve"*, **cannot become one until a function type
constructor exists.** The two questions:

**4a. Is `(_) => (_)` — a function/delegate type — part of step 4's vocabulary,
or later?** Note the spelling collision it introduces: a bare `text => number`
type and the `=>` inside `lookup text => number` share the arrow, and *that*
collision is what makes §2 ambiguous rather than what makes it broken. Adding the
constructor is what turns the §2 case from "unknown type" into "ambiguity with
three repairs" — which is the ruled behaviour. So this is less "an extra type" and
more "the thing §2 was written about."

**4b. If it is later, do you want `LOOKUP-ARROW` §2 held until then?** I would
land step 4 with §1's common-shape case as the maintained test (it works and is
the strongest one-table argument we have), and tag §2 in `Test/Expiry.cs` against
the arrival of `(_) => (_)`, rather than shipping a test that asserts "unknown
type" for a line the ruling says is a three-way ambiguity — that test would
encode the gap as if it were the design.

**Recommendation:** if `(_) => (_)` is a small addition in your view, fold it in
and §2 lands with §1. If it carries weight I cannot see (variance, how a function
type unifies, whether it is spelled with `=>` or guarded), rule it *later* and I
will hold §2 with an expiry tag. Either way I need to know before writing the
lookup tests, because the two answers want opposite assertions for the same
input.

---

## 5. Decided by existing rulings — veto if I have misread

These I am *not* asking, but stating so a wrong reading is caught now rather than
in a commit:

- **Bare `list`, `lookup`, `optional` are not types.** The type is `list of (_)`;
  `list` alone is a no-reading, hence unknown-type. This is why the
  `NameShadowing` fixture's `=> list` becomes `=> list of number` in the sweep. If
  a bare `list` should mean anything, that is news to me.
- **`truth` is a type; `true`/`false` are its value literals.** `truth` is in
  `TypeNames`, `true`/`false` in the value `Truths`. `x is true` is a value; a
  `=> truth` annotation is a type.
- **`money` is not a primitive.** `TYPE-VOCABULARY` §2 — a strong alias over
  `number`, a later feature. In the sweep, a fixture's `money` becomes either
  `number` or a local `type money;`, preserving the fixture's intent, not a new
  primitive.
- **`type X;` with no body is usable as an annotation.** `TYPE-VOCABULARY` §3 — a
  declaration names it; a definition would give it structure; an opaque handle is
  a real thing and the table stays one-state.
- **The action type still has no surface spelling.** `FIVE-RULINGS` §2b. Nothing
  in step 4 lets a user write it.

---

## 6. Draft of the new finding, for you to sharpen

Diagnostics are the teaching mechanism here, so the wording is yours in the end.
My starting point, in the house voice:

> `UnknownType` — «money» is not a type. Nothing in scope declares it and the
> language supplies no such type. Declare it with «type money;», or use a type
> that is in scope.

It points at the annotation's span, quotes the words as written, and — unlike a
value no-reading — says the one true cause, because for a type there is only one:
the name is not in the table. No repair list, because there is no bracketing that
turns a missing name into a present one; the fix is a declaration or a different
word, and both are the author's.

---

## 7. Summary

| # | question | my recommendation |
|---|---|---|
| 1 | `fast number` spelling | **modifier on `number`** — one number type, the checker never sees two. Reserved word is spent either way. Not blocking; can defer |
| 2 | annotation scope | **annotations only**; flag `type X = Base` bases as a clean follow-up. Every sweep fixture is an annotation |
| 3 | kind filter's reach | **suppress value-only readings** (literal, operator, `old`, `[ … ]`) so non-types are unknown-type findings; **defer arity/multiplicity** (tuple or keyed group filling a hole) to the checker. Confirm the line |
| 4 | `(_) => (_)` function type | **not in the prelude today.** `LOOKUP-ARROW` §1 works; §2's ambiguity **cannot arise without it**. Rule it in (and §2 lands) or later (and I hold §2 with an expiry tag) |

Answers to 1–4 change the code directly; 5 only if I have misread; 6 is a
starting point. I will start on the parts none of these touch — the `SymbolTable`
projections and the resolver's kind mode — while these are with you, unless you'd
rather I hold entirely.
