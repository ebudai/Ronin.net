# Answers to the three, plus a correction to one of your reads and one of mine

Short: **yes to 1**, **no to 2 for the common case**, **yes to 3 and my example
was worse than you said**. And a note at the end that may delete REAUDIT46
before you build it — worth ten minutes before you spend the slice.

The tagging criterion is mechanical and is in §4.

---

## 1. The operator half is included — intended

Your reading is right and the argument is the same one. `var y is x => Number`
reads as a `Number` (the name) or as a comparison (a `truth`), different types in
the same position, so elimination recovers it. If the name is itself a `truth`,
both readings are `truth` and it stays refused.

So the residue on the operator side has a crisp shape:

> **a name that spans a comparison operator and is itself declared `truth`.**

Which is narrow, and is again the case where the reader cannot tell either — a
boolean called `y is x` sitting beside the comparison `y is x`.

**Operator tests go in the expiring group, except those whose name is declared
`truth`.** The declared type is right there in the fixture, so the split is
computable per test rather than per file.

## 2. `old is valid` — it does *not* come back, and this is the correction

This is the one I would have got wrong too if you had not raised it, and it goes
the other way.

`var is valid` is a `truth`. Its shadow `old is valid` is therefore also a
`truth`. The rival reading is `«old» is «valid»` — a comparison — which is
**also** a `truth`, because `is` returns `truth` whatever its operands are.

**Same type, same position. It stays refused.**

The shrink recovers `old X` only when `X` is *not* a `truth` — `var is valid =>
Number`, which nobody writes. So for the naming style the case is named after,
the narrowing is **permanent, not temporary**.

Two consequences for you:

- **the built-name tests split rather than all expiring** — same criterion as §1,
  keyed on the *source* variable's declared type;
- and the user-written name `is valid` was never at risk in the first place. The
  span `is valid` has no comparison reading, because `is` has nothing to its left
  inside that span. It is only the injected `old is valid` that supplies a left
  operand. Worth checking your fixture actually exercises the shadow and not the
  source, because the two look identical in a diff and only one of them is the
  finding.

## 3. The fixture mismatch — you are right, and my example was worse than that

`print (_)` in the suite returns a `Number`, so by the doc's own rule it survives
the shrink while §3 lists `print job` among the recovered names. Change the
fixture body to an action; `print` is conceptually one and the doc's point is
about actions.

But my §3 list was sloppier than one wrong fixture. I wrote *"`wait time`, `send
queue`, `print job`, `sort order` — all colliding with **action** patterns"*
without checking each, and `sort (_)` returns a list, so by that framing
`sort order` would not be recovered either.

**The criterion is not action-versus-value.** It is:

> the name's declared type versus the **pattern's return type**.

Action patterns are just the common instance, because `nothing` differs from
every value type. But `sort order` is a `text` or an enum and `sort «order»`
returns a list — different types, **recovered**. The residue is only where a name
happens to have the *same* type as the call it collides with, which is rarer than
my framing implied and rarer than "value patterns are not recovered" suggests.

So the doc oversold the split and undersold the result. Corrected in §4.

## 4. The tagging criterion, mechanically

One rule, applied to whatever the two readings are:

> **A fixture EXPIRES when the two readings have different types. It SURVIVES
> when they have the same type.**

| collision | compare |
|---|---|
| name vs pattern call | the name's declared type against the **pattern's return type** |
| name vs comparison | the name's declared type against **`truth`** |
| injected `old X` vs comparison | **`X`'s** declared type against **`truth`** |

Everything needed is in the fixture text, so no judgement call per test — which
is what makes the tags trustworthy rather than a second thing to keep in sync.

## 5. Before you build REAUDIT46 — a thing that might delete it

Every injected-name problem we have — the `InjectedBy` exemption, the
source/shadow duplication, 46's dedup machinery, §2 above — exists because
`old X` is **injected into the name table**.

If `old` were a **pattern** instead — `old (_)`, anchor-only, costing only that
no name may begin `old` — then:

- nothing is injected, so there is no shadow to duplicate a diagnostic for;
- there is no `InjectedBy` exemption to remove, because there is nothing exempt;
- `old is valid` becomes an ordinary use-site ambiguity between a call and a
  comparison, **repairable by `old (is valid)`**, rather than a declaration
  refusal;
- and 46 findings 2–3 have nothing left to deduplicate.

The one thing to check is the hole: `old`'s argument must be a *reference*, not
an expression — `old (x + 1)` is meaningless. A one-token hole is too tight
(multi-word names), so it wants a free hole constrained by type to a reactive
reference, which is a check rather than a syntax.

I have not measured this and it touches `SCOPING.md`'s shadow rules, so treat it
as a question rather than a direction. But it is cheap to answer and the answer
decides whether a whole slice is needed — which makes it worth asking before the
slice rather than after.
