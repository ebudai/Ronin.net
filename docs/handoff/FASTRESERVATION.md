# `fast`, and a modifier that may begin a name by design — findings 2 and 3

> **Ledger** — `[R]` `fast`, and a modifier that may begin a name by design — findings 2 and 3
> supersedes: none
> superseded by: none

**From:** the successor, actioning FRESHAUDIT21. **State:** findings 4, 5, 6 and
the 2c census are fixed and pushed (`c797b5f..a1edd99`). Findings 2 and 3 are
here rather than in a commit because both collide with behaviour the language
tests on purpose, and the way out is a ruling, not a guess.

---

## Finding 2 — reserve `fast`, but a modifier is *allowed* to begin a name

The audit is right about the witness: `var fast number => number = 1` declares a
usable value name `fast number`, and `return fast number` reads it. And
`TYPEHALFRULINGS` §1 did say `fast` "goes in the table whatever else it is."

But the same mechanism is **tested as correct** one modifier over:

```csharp
// Test/Unit/Loops.cs:90 — "a modifier may still begin a name"
//   The rule is about keywords that ANNOUNCE a production, and a modifier
//   in this position announces nothing — «var hidden cost» is a name the
//   language already accepts, so the fix must not take it away.
var datum = Assert.IsType<Datum>(Only("var hidden cost => number;\n"));
Assert.Equal("hidden cost", datum.Identifier.Words);
```

`Name.Parse` refuses a **production-announcing** keyword at the head of a name and
lets a **modifier** through, precisely because a modifier there announces
nothing (`Grammar/Name.cs:74`). So `hidden cost`, `reactive score`, and
`fast lane` are all names by the same rule, and I confirmed `fast` and `reactive`
behave identically in every position. Removing the modifier exception fails the
test above — the allowance is deliberate.

So finding 2 is not "the fix was forgotten." It is a **collision between two
rules**, and which gives way is yours:

**The distinction I can see** is that `fast` is the only **type-representation**
modifier — `fast number` *is* a number type — so a name `fast number` shadows a
**type spelling**, where `hidden cost` shadows nothing a type could be written as.
That would make `fast` uniquely worth refusing at a name head, while
`hidden cost` stays legal. But that is my inference from the rulings, not
something they say, and it is exactly the kind of thing that should be decided
rather than assumed.

**Question A — scope.** Is `fast` refused as a name-lead *specifically* (the
other modifiers keep the `hidden cost` allowance), or is the allowance itself
being reconsidered for every modifier (which retires the tested behaviour above)?

**Question B — mechanism.** The registry the name rules consult is
`SymbolTable.Whole`, derived from `Supplies`, and a `Supplies` entry carries a
`SymbolKind` of `Value` or `Type` — a modifier is honestly neither, so putting
`fast` there the way the audit suggests needs a **third kind** (a non-resolving
`Reserved`) or a flag. And `Whole` matches a *whole* name, so it would refuse the
bare word `fast` and not `fast number`; refusing the name needs a check on its
**first word**. Roughly:

- **(a)** add `SymbolKind.Reserved`, a `fast` descriptor, and a first-word refusal
  in `Declarations` — puts `fast` in the docs and the name-rule table as the audit
  asks, at the cost of a new kind and a modifier that lives in two registries;
- **(b)** refuse `fast` at a name head in `Name.Parse` directly — smallest change,
  keeps `fast` out of the value/type registry it does not belong in, but the
  message is "expected identifier" rather than "`fast` is reserved", and it is not
  "in the table" the way §1's words read.

I lean (a) for the message and the documentation, (b) for honesty about what
`fast` is. I will build whichever you rule, once A settles what it is refusing.

---

## Finding 3 — `fast` validates nothing, and most of that is the checker's job

The audit's probes all compile clean: `fast truth`, `fast list of number`,
`fast fast number`, `fast if true`, `fast type box`. Two different things are
tangled here:

**The target check — `fast` only on `number` — is checker work.** `fast truth`
is wrong because `fast` qualifies a *number* occurrence, and knowing the
annotation resolved to `truth` rather than `number` is exactly the resolved
semantic type that **finding 1** says does not exist yet. The annotation walk
strips the modifier before resolving, so today it genuinely cannot tell
`fast number` from `fast truth`. This belongs with the checker that consumes
resolved types, beside every other rule in finding 1 — not as a text pass now.
Duplicate `fast` is the same shape: a fact about the modifier set on one typed
occurrence.

**The placement check — `fast if`, `fast type box` — is a general modifier
question, not a `fast` one.** `Modifiers.Parse` consumes any run of any modifier
in front of scopes, loops, conditionals and declarations, and validates the
placement of **none** of them for **any** modifier — `hidden if true` compiles as
cleanly as `fast if true`. Fixing it for `fast` alone would single out the newest
modifier for a rule the others dodge; fixing it properly is a ruling about where
modifiers may appear at all, which is broader than this slice.

**Question C.** Is `fast`'s target/duplicate validation part of the checker slice
(finding 1), tagged and waited on with the rest? I recommend yes.

**Question D.** Is modifier *placement* (any modifier on any scope) a separate
general concern to raise on its own, rather than something `fast` should carry? I
recommend yes, and can write it up separately.

---

## Summary

| finding | state |
|---|---|
| 4 grouped type unreachable | **fixed, pushed** |
| 5 keyed group rejected | **fixed, pushed** |
| 6 changed-file whitespace gate | **fixed, pushed** — whitespace-only commit |
| 2c keyword census | **fixed, pushed** — `fast` added |
| 2 reserve `fast` | **needs a ruling** — collides with the tested "a modifier may begin a name"; scope (A) and mechanism (B) are yours |
| 3 validate `fast` | **needs a ruling** — the target check is checker work (finding 1); placement is a general modifier question, not `fast`'s |
| 1 no semantic checker | the next slice — a resolved type term the walk stores, then inference and unification |
