# `lookup text => number` — taken, and four things it needs

> **Ledger** — `[V]` `lookup text => number` — taken, and four things it needs
> supersedes: none
> superseded by: none

Ruled. Zero reserved words, prose-shaped, brackets only where a reader would
want them anyway.

Before building it I checked the shape that matters most — the ordinary
declaration has **two** arrows in it — and it is safe, for a reason worth
knowing.

---

## 1. The commonest shape is unique, and the kind filter is why

```
  m => lookup text => number               1 reading   ascribe(m : LOOKUP[text -> number])
  m => text => number                      1 reading   ascribe(m : DELEGATE(text -> number))
  m => lookup text => number => truth      3 readings  AMBIGUOUS
```

Two arrows three tokens apart, and no ambiguity — because **ascription's left
operand is a name and the type arrow's operands are types.** The kinds differ, so
only one reading is kind-correct.

That is `FIVE-RULINGS` §4 doing load-bearing work for the third time: overload
narrowing, then type-position resolution, now this. **If types and values were
two tables this would not resolve**, which is worth noting in the commit, because
it is the strongest concrete argument for the one-table ruling anyone has
produced so far.

## 2. The ambiguous case errors, with three repairs — not two

```
  m => lookup text => number => truth
      ascribe(m : LOOKUP[text -> DELEGATE(number -> truth)])
      ascribe(m : DELEGATE(LOOKUP[text -> number] -> truth))
      ascribe(m : LOOKUP[DELEGATE(text -> number) -> truth])
```

All three are reachable, so it is repair-complete:

```
  lookup text => ( number => truth )      a table of callbacks
  ( lookup text => number ) => truth      a function taking a table
  lookup ( text => number ) => truth      a table keyed by functions
```

Verified for the middle and last; the first is the same shape. The generated
diagnostic should offer all three as selectable, per `AMBIGUITY-AS-ERROR`.

## 3. Do NOT give the arrow a binding power that resolves this

This is the thing most likely to be done by reflex and it would be wrong.

`STOP-AND-LADDER` §3 puts ascription at the loosest rung, and a precedence or a
right-associativity rule *would* make `a => b => c` parse silently. **Resist it
here.** The three readings above do not differ in operator tightness — they
differ in **which constructor claims which arrow**. Precedence exists to order
operators of different binding strength; using it to choose between two
constructors competing for one spelling is a silent pick, and it is the one thing
`cost may order suggestions, never choose among them` was written to prevent.

So: the arrow keeps its loosest rung for the cases where tightness is the
question, and a run with more than one arrow whose readings differ by
*constructor* is an ambiguity error.

## 4. Rename the token

`Delegate.cs` reads `parser.TryAdvance<Returns>()` — the `=>` token class is
called **`Returns`**, named after one of what are now three jobs:

```
  ascription   var n => number
  delegate     x => { … }   and   () => Number
  lookup type  lookup text => number
```

A token named for one of its uses is the same trap as `LexemeKind.Symbol` being
asked to carry `=`. **Call it `Arrow`.** One rename, and the name stops lying
before a fourth job arrives.

## 5. The prelude, and the asymmetry

```
  list of (_)                unchanged
  optional (_)               unchanged
  lookup (_) => (_)          replaces  lookup of {_} {_}
```

`list of number` beside `lookup text => number` is asymmetric, and it is
justified by meaning rather than left as an accident: `of` reads correctly for
**one** parameter, and an arrow reads correctly for a **mapping**. `list => number`
would be wrong and `lookup of (text) (number)` reads like a form field. The
spelling follows the shape of the thing.

## 6. Summary

| | |
|---|---|
| `lookup text => number` | **taken.** Zero reserved words — a symbol cannot be captured by a name |
| the two-arrow declaration | **unique**, measured. `m => lookup text => number` has one reading |
| why | ascription's left operand is a **name**, the type arrow's are **types** — the kind filter. `FIVE-RULINGS` §4's best argument yet |
| the three-arrow case | **ambiguity error**, three readings, all three bracketable and all three offered |
| binding power | **do not use one to resolve it.** The readings differ by constructor, not by tightness — precedence there is a silent pick |
| the token | rename `Returns` → **`Arrow`**. Three jobs, and the name describes one |
| prelude | `lookup (_) => (_)`; `list of (_)` and `optional (_)` unchanged |
| the asymmetry | deliberate — `of` for one parameter, an arrow for a mapping |

Probe: `lookup_arrow.py`.
