# Two answers: write sets, and the numeric tower

> **Ledger** — `[V]` Two answers: write sets, and the numeric tower
> supersedes: none
> superseded by: none

---

# Multi-writer `when`

**Implement `Cascades.Writers(...)` as a pure function over supplied sets.
Leave derivation until the analyser exists.**

Three reasons, and the third is the one that matters:

**Consistency.** Tier-1 cycle detection already shipped in exactly that shape,
tested directly. Two consumers of the same missing data should not be built
two different ways.

**Scope.** A day versus pulling in the call graph, for a rule whose *content*
is already settled. The rule can be right and tested before the data exists.

**The derivation is not cascade infrastructure — it is a shared effect
analysis, and it has four consumers, not one:**

| consumer | property it needs |
|---|---|
| multi-writer `when` | write sets, transitive, with provenance |
| tier-1 cycle detection | read **and** write sets, transitive |
| purity enforcement | does this call touch a var or a resource |
| error-ness freezing | can this call produce an error |

All four are least-fixed-points over the same call graph, differing only in the
lattice being propagated. Built inside `Cascades`, the first one buries a
general analysis in one consumer and the other three re-derive it. Built once
as an effect pass, all four fall out of one traversal and one recursion-handling
strategy.

So: not now, and when it comes, not in `Cascades`.

**One thing to do now, though.** Give `Writers` the signature the derivation
will eventually need, even though the tests supply it by hand:

```
Writers(IReadOnlyDictionary<WhenId, IReadOnlyCollection<Write>>)
    where Write carries { Cell, AttributedTo }
```

Provenance is the requirement that will otherwise be discovered late, because
the error names the `when`, not the function that did the writing. Encoding it
in the shape now means the derivation slots in without touching the consumer.
Today `AttributedTo` is just the `when` itself.

---

# The numeric tower

## 1. What is a trap, concretely

**An `Error` value. Catchable, flows through the graph, `otherwise` handles
it.** Not a `Fault`, not a hard stop.

My word choice misled — "trap" meant *not silent*, not *fatal*. Loud and
catchable are compatible.

The reasoning matters for the message. Overflow here is neither a program
mistake nor an interpreter bug: under the finished design the answer exists and
would widen to arbitrary precision. It is a **documented limit of the current
implementation**, which belongs on the catchable side — the user can respond,
by restructuring or by opting into `fast number`. A `Fault` would be
uncatchable, and this is anticipated rather than a defect.

So the message must say *limit*, or people will go debugging their arithmetic:

```
error: 9223372036854775807 * 2 exceeds the current 64-bit range.
       Ronin numbers are exact and unbounded by design; arbitrary precision
       is not implemented yet. Use `fast number` for approximate arithmetic
       at this magnitude.
```

## 2. Decimal literals — there is a third option, and it is free

`4.5` is a **decimal**: a scaled integer, `units × 10^-scale`. Not a general
rational, and not a required annotation.

The dilemma dissolves because **decimals are closed under `+ - *` among
themselves**, so they never touch gcd:

```
a·10⁻ⁱ + b·10⁻ʲ  =  (a·10^(j-i) + b)·10⁻ʲ     for i ≤ j
a·10⁻ⁱ × b·10⁻ʲ  =  (a·b)·10^-(i+j)
```

Only **division** leaves the cheap path. Measured, same workload, two adds and
a multiply per element:

```
representation                      ns/elem   vs int64
int64                                  0.28      1.00x
double (fast number)                   0.62      2.21x
decimal (scaled int64)                 0.23      0.83x
rational (gcd-normalised)             46.89    166.29x
```

Decimal arithmetic is free — indistinguishable from int64, and *faster than
double*. General rationals are **166×**, which is exactly why they must be the
rare path rather than the default. Your instinct that rational-by-default was a
real performance change was right; it was worse than you feared.

**The representation ladder**, each step entered only when an operation demands
it:

```
int64  →  decimal (scaled int64)  →  rational (int64/int64)  →  Error
```

`42` is an integer. `4.5` is a decimal. `1/3` is a rational. `0.1 + 0.2` is
exactly `0.3`, at int64 speed, which is the whole promise of the numeric design
delivered in the common case for free.

## 3. Mixing exact and fast

**Contagion inside an expression; explicit at a declared boundary.**

Within an expression, `exact + fast → fast`. Someone already opted into
approximation *somewhere* in that expression, and propagating it is the honest
consequence of their choice. Requiring conversion at every operator would make
`fast` viral in a worse way than contagion is.

At a declared boundary it is checked:

```
var x => number = <expression producing fast>;      // error
var x => fast number = <expression producing fast>; // fine
```

This is the same shape as error-ness and nullability, which are both inferred
inside and declared at boundaries. Third instance of the pattern — worth
noticing that the language keeps converging on it.

## 4. Does `6 / 3` yield integer 2

**Yes, integer 2. And the worry behind the question dissolves.**

"The type of a division isn't statically known" — but `number` is **one type**.
The *representation* varies; the type does not. A division statically has type
`number`, always. Which representation carries it is a runtime property, fixed
statically only where range analysis can prove it.

That is not a compromise, it is the design: the compiler owns representation,
the programmer sees one exact numeric type. Normalising to the narrowest
representation is what keeps a single exact division from infecting a whole
computation with rationals.

## 5. Literal syntax and lexing

**The lexer distinguishes integer from decimal**, and it is a purely lexical
property — the presence of a `.`. Emit the kind and the raw text; let the
evaluator construct the value. Nothing here needs the symbol table.

**No rational literal syntax.** `1/3` is division of two integer literals and
evaluates exactly. A literal form would buy nothing and add a lexeme.

**But there is a real defect in the current lexer, and it is not fixable
further up.** `Numeric.Lex` accepts `7,000,876` with comma separators, and
comma is also the argument separator:

```
f(1,234)      one argument 1234?  or two arguments 1 and 234?
```

That ambiguity lives **below the symbol table**, so minimum-lookup cannot
resolve it — the resolver never sees a choice, because the lexer already made
one. Greedy lexing silently picks the number.

**Use `_` for digit separators**: `7_000_876`. Rust, C#, Java, Python and Swift
all chose it, and this is exactly why. This is worth changing before anything
depends on the current spelling.
