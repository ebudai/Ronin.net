# Left-to-right works — and `WHYSYMBOLINFIX.md` was running on a bug of mine

> **Ledger** — `[V]` Left-to-right works — and `WHYSYMBOLINFIX.md` was running on a bug of mine
> supersedes: not yet checked
> superseded by: not yet checked

**Withdraw `WHYSYMBOLINFIX.md`.** Its demonstration was a defect in
`dp_resolver.py`, not a property of the language, and the conclusion drawn from
it — that word infix needs per-pattern binding powers and a restructured table —
is wrong. Budai's proposal works, and it is cheap.

---

## 1. The defect, and the port caught it while the reference did not

```
a + b + c        ->  NO PARSE
a - b - c        ->  NO PARSE
a + b + c + d    ->  NO PARSE
```

Every same-precedence chain was unwritable in `dp_resolver.py`, and no test
covered one. Cause: for a left-associative operator the code handed **both**
operands the minimum `bp + 1`, so neither side could contain the operator's own
level. Correct precedence climbing gives the repeating side `bp` and the other
`bp + 1`. Fixed; all six chains now resolve, `a + b * c` still groups as
`(a + (b * c))`.

**`Resolver.cs` does not have this bug.** `:488-501` is correct, and its comment
names the exact failure:

> Handing both sides the higher minimum forbids the operator on either, and a
> chain of one precedence stops parsing altogether.

So the programmer found and fixed it while porting, and wrote down why. The
reference kept it for months. Worth recording plainly: **the port is the correct
artifact here and the reference was the defective one** — the inverse of the
direction this project has usually run, and a reason not to treat
`dp_resolver.py` as authoritative just because it came first.

## 2. What that invalidates

`WHYSYMBOLINFIX.md` §1 showed `a + b * c` becoming `NO PARSE` when binding
powers were equalised, and concluded that *precedence* is what allows symbol
infix. It was showing the bug. With associativity correct:

```
equal binding powers, left-associative

  a + b * c        ->  OK   ((«a» + «b») * «c»)
  a * b + c        ->  OK   ((«a» * «b») + «c»)
  a + b * c - d    ->  OK   (((«a» + «b») * «c») - «d»)
```

Unique, left to right, no ties. **Equal precedence was never the problem. The
absence of associativity was.**

## 3. Your proposal, measured on word patterns

Applying the same rule to the word layer — a **leading** hole parsed at
`pattern_bp`, a **trailing** hole at `pattern_bp + 1`, one shared level for all
patterns:

```
patterns «(_) to (_)» and «(_) of (_)», names a b c d

  a to b            ->  OK    ((«a») to («b»))
  a to b of c       ->  OK    (((«a») to («b»)) of («c»))
  a of b to c       ->  OK    (((«a») of («b»)) to («c»))
  a to b of c to d  ->  OK    ((((«a») to («b»)) of («c»)) to («d»))

the same statement under the shipped rule:
  a to b of c       ->  TIE -> ERROR
```

**It works.** No per-pattern binding powers, no restructured table, no new index
in the DP — one associativity convention, using machinery that already exists
for symbols.

And no, it is not chaos: **Smalltalk has shipped this since 1980.** All binary
selectors share one precedence and evaluate left to right, `3 + 4 * 5` is 35,
and its keyword messages are the closest thing in any language to the
composability you are after.

## 4. What it does *not* settle

```
patterns «sorted (_)» and «(_) reversed»

  sorted xs reversed  ->  TIE -> ERROR      still
```

Associativity settles infix∘infix because there is a left and a right to
associate. Prefix∘postfix has neither: one operator sits wholly to the left of
its argument and the other wholly to the right, so there is no chain to grow in
a direction.

Settling *that* needs a different rule — a tie-break preferring the derivation
whose leftmost operation binds first — and **I would leave it a tie error.**
`a - b - c` grouping leftward is a convention every language shares and every
reader already has. `sorted xs reversed` is a genuine semantic fork — sort then
reverse, or reverse then sort — that a reader cannot infer from the text, so a
bracket there is information rather than ceremony.

That is a taste call and it is yours. The mechanisms are independent: you can
take §3 without §4.

## 5. So the revised cost of "all combos"

| shape | blocked on |
|---|---|
| prefix | nothing |
| postfix | last-word index, suffix-free R6, the port divergence in `POSTFIXDIAGNOSIS.md` §1 |
| **infix** | the same two, **plus** the left-associativity convention in §3 — which is small — and the index problem, which is not |

The index is now the real obstacle for infix, not precedence. An infix pattern
`(_) to (_)` keys on neither its first token nor its last, so it cannot be found
the way anchored and postfix patterns can. The candidate is to index it by its
**glue word** and scan for that word within the span — which is exactly what the
symbol layer already does for operators, and is available because R5 reserves
glue words so no name can contain one.

That is a real design, and it is the thing to work out next if you want infix.
It is also considerably less alarming than "restructure the DP table", which is
what I told you an hour ago on the strength of my own broken benchmark.

## 6. On composability

You are right that it is the superpower, and this makes it much more attainable
than `WHYSYMBOLINFIX.md` implied. Worth saying explicitly since I spent a
document arguing the other way: with left-associativity, a language of prefix,
postfix and infix word patterns composes without brackets in the common cases
and asks for them only where a reader would want them too.
