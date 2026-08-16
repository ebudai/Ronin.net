# `if` as an expression — yes, and it is cheaper than what it replaces

> **Ledger** — `[V]` `if` as an expression — yes, and it is cheaper than what it replaces
> supersedes: none
> superseded by: none

Checked with `if_expression.py`, 5/5. Short version: this is not merely a nicer
spelling, it **refunds two reserved words**, and it lets one `otherwise` do a
job we were about to duplicate.

---

## 1. It pays for itself in the glue registry

```
if (_) then (_) otherwise (_)     glue = {then, otherwise}     two reserved words
if (_) {_}                        glue = {}                    nothing
if (_) {_} otherwise {_}          glue = {}                    nothing (refined)
```

The third line is worth pausing on: even the two-armed braced form costs
nothing, because `otherwise` sits between two braced blocks and no name can
straddle a bracket. So the braced shape is free under either reservation policy
— it does not depend on the refinement, which means it can ship without waiting
on anything.

`then` and `else` stop existing. That is two names given back to every program
(`then` is rare in names; `else` less so, but both were pure cost).

## 2. The condition is determinate, which is not obvious and does matter

The condition is a *free* hole, so the natural worry is that it runs past the
brace. It cannot:

```
if a ( c )        OK   [if «a» «c»]           unique
if a b ( c )      OK   [if «a b» «c»]         unique — multi-word condition
if a + b ( c )    OK   [if («a» + «b») «c»]   unique — operators fine
```

**A free hole followed by a bracketed hole is determinate in extent**, even
though a free hole alone is not — the bracket fixes where the condition must
stop. That is the same property that makes the braced shape free of glue, and
it is worth naming because it generalises: any construct that ends its
arguments with a block gets both benefits at once.

Nested `if` in a condition is legal and unique (`if if a ( b ) ( c )` resolves
one way) but unlovely. Formatter problem, not a grammar problem — lint it,
don't rule it out.

## 3. One `otherwise`, not two — the composition to take

`if c { a }` with no alternative has no value when `c` is false. The language
already has `nothing` and `optional T` for exactly that. So:

```
if c { a }                     optional T
if c { a } otherwise { b }     T
```

and **the second is not a new form.** It is the postfix `otherwise` the language
already has for catching `nothing` and error, applied to an optional. One word,
one meaning: *when the left side produced nothing, use this instead.*

The conditional and the error-handling `otherwise` become the same operator
rather than two words that happen to rhyme. That is a real economy — it is the
same shape of win as minimum-lookup subsuming maximal-munch.

**So: no `else`.** And `otherwise` stays a parser-level infix form, which is
where `R6-AND-INFIX.md` already put it.

---

## 4. The one thing I would not copy from Rust

Rust's rule is that `{ x }` has value `x` and `{ x; }` has value `()`. **One
character, invisible on screen, changes the type of the block.** It is Rust's
most-complained-about papercut, and for a language whose first principle is
readability at the cost of writability it is the wrong trade — writability is
exactly what the trailing semicolon buys.

Keep the sugar, delete the invisibility. Position decides the requirement and
the compiler says so:

| block position | rule | if violated |
|---|---|---|
| expression position | must end in an expression, no trailing `;` | finding: *this block is used as a value but ends with «;», which discards it* |
| statement position | a final bare expression has its value discarded | finding **only when the value is not `nothing`**: *this value is discarded — did you mean to return it?* |

Both of Rust's silent cases become diagnostics. Nobody ever has to notice a
semicolon to know what a block means.

The second row is the one that catches the real mistake — a function that
computes its answer and forgets to yield it. Suppressing it for `nothing`-valued
expressions keeps ordinary effectful statements quiet.

## 5. `return` stays

Early exit is a different thing from a block's value, and `if c { return 3; }`
should keep working exactly as it does. The final-expression rule is sugar for
the common case, not a replacement for control flow.

Same rule everywhere — function bodies included, so `function double (x) { x * 2 }`
works. With the §4 diagnostics, a body whose declared result is `nothing` but
which ends in a value gets told.

## 6. Two consequences worth naming before implementation

**`if` can no longer be statement-only.** `Statement.Parse` currently routes
`if` as a production; it now has to be reachable from expression position too,
and the same node has to serve both. That is the main implementation cost of
this change and it is worth doing deliberately rather than discovering.

**Conditional dependencies in `let`.** `let x => if c { a } otherwise { b }`
reads `c` always and exactly one of `a`/`b` per evaluation. The graph already
clears and rebuilds a node's dependencies on every recompute, so a disappearing
edge is handled — but it is now reachable from ordinary user code rather than
only in principle, and it is a *feature*: the branch not taken is not a
dependency, so writing to it wakes nothing. Worth a test that asserts exactly
that, because it is the kind of thing an optimisation could quietly break.

---

## 7. Tests

| # | case | expect |
|---|---|---|
| 1 | `if c { a }` in expression position | value `a`, type `optional T` |
| 2 | `if c { a }` when `c` is false | `nothing` |
| 3 | `if c { a } otherwise { b }` | type `T`, both arms |
| 4 | `if c { a; }` in expression position | the trailing-semicolon finding |
| 5 | `{ a; b; c }` | value is `c` |
| 6 | statement-position block ending in a non-`nothing` expression | the discarded-value finding |
| 7 | statement-position block ending in a `nothing` expression | silent |
| 8 | `function double (x) { x * 2 }` | returns `x * 2`, no `return` needed |
| 9 | `if c { return 3; }` | early exit still works |
| 10 | multi-word condition, `if a b { c }` | condition is `a b`, unique |
| 11 | operator in condition, `if a + b { c }` | unique |
| 12 | nested `if` in a condition | resolves; lint, not error |
| 13 | glue registry after the change | `then` and `else` absent; still `## RESERVED (0)` |
| 14 | `let x => if c { a } otherwise { b }`, write to the untaken arm | no recompute |

Test 14 is the one that will be broken by accident later.
