# `old x` — your instinct is right, the scope does not do it, and here is why

> **Ledger** — `[R]` `old x` — your instinct is right, the scope does not do it, and here is why
> supersedes: not yet checked
> superseded by: not yet checked

Getting it out of the flat name table is the right move. But the scope version
has the **same defect as the injection**, and the reason is worth having because
it generalises: the fix is not *where the thing lives*, it is *whether it has a
bracketable form*.

Measured, on `old is valid` with `is valid` declared:

```
  1. flat injected NAME «old is valid»
        («old» is «valid»)      selectable
        «old is valid»          NOT SELECTABLE      -> UNREPAIRABLE

  2. PATTERN «old (_)» over the name «is valid»
        («old» is «valid»)      selectable
        old «is valid»          selectable          -> all repairable

  3. SCOPE «old», member «is valid», juxtaposed
        («old» is «valid»)      selectable
        old·«is valid»          NOT SELECTABLE      -> UNREPAIRABLE
```

---

## 1. Why the scope does not help

A scope-qualified reference written by juxtaposition — `old` then `is valid` —
is still a **bare run of words**. Brackets group; they do not classify. So
`old (is valid)` does not mean "the scope member"; it means whatever `old (…)`
means, which under scheme 3 is nothing. There is no spelling that selects it.

That is the same reason a flat name fails. Moving the entry from the name table
to a scope table changes the bookkeeping and not the syntax, and the syntax is
where the problem is.

**A call is different because it has an argument, and an argument can be
bracketed.** `old (is valid)` puts the `is` inside a bracket where the comparison
cannot reach it, so the call reading becomes uniquely selectable — and the
comparison stays selectable as `(old) is (valid)`. Both readings expressible,
which is the standard the whole ambiguity-as-error design rests on.

So the general form, which is worth writing down because it will decide the next
one of these too:

> **A construct needs a bracketable form to survive ambiguity-as-error.** Names
> and juxtaposed qualifications have none. Calls, and anything with a
> bracket-delimited part, do.

## 2. There is a version of your scope idea that works

If qualification used a **symbol** rather than juxtaposition — `old.x`, or
whatever separator — there is no ambiguity at all, because no name and no
operator can span a symbol. That is scheme 4, and it is sound.

What it costs is the spelling. `old is valid` becomes `old.is valid`, which reads
worse than either alternative and introduces a member-access notation for
something that is not a member. I would not take it, but it is on the table and
it is the only version of the scope idea that holds.

## 3. What the pattern costs

`old (_)` is anchor-only, so under the self-ambiguity rule **no name may begin
`old`**:

```
  names beginning «old»        534   0.157%      old_x, old_state, old_value
  names beginning «previous»   111   0.033%
  names beginning «last»       481   0.141%
```

0.157% — about four times what `wait` would have cost, and the highest
single-word reservation we have measured. `old value`, `old state`, `old price`
are names people write.

Worth knowing, and worth noting it is **not new**: the flat injection already
made every `old X` collide with a user name `old X`, so the space was already
half-taken. What changes is that the collision moves from a runtime name clash to
a declaration-time refusal with a clear message.

If 0.157% is too much, `previous (_)` costs a fifth of it and reads as well.
Your call — but I would not decide it on the corpus alone, because `old` is short
and this language's whole premise is that the common case should read well.

## 4. What it buys

Everything downstream of injection stops existing:

- no `InjectedBy` exemption, because nothing is injected;
- no source/shadow duplicate diagnostics, because there is no shadow;
- **`REAUDIT46` findings 2–3 have nothing left to deduplicate** — that is a whole
  slice that may not need building;
- `SCOPING.md`'s shadow-suppression table becomes dead;
- and `old is valid` stops being a declaration refusal and becomes an ordinary
  use-site ambiguity, repairable in both directions.

## 5. The constraint is cheaper than I said — correction

I wrote that the argument constraint "needs the type/kind machinery that does not
exist yet". **That is wrong**, and Budai's point is the reason: `old` only ever
applies to reactives, and *reactive* is a property the symbol table already
records — it has to, or the graph could not be built.

So the check is two structural questions, both answerable at resolve time with
nothing new:

1. did the hole's derivation come out as a **bare name reference** — not a call,
   not an operator expression;
2. is that name **reactive**.

No types involved. `old (x + 1)` fails (1); `old y` where `y` is a plain constant
fails (2); and both get a message that says which.

**So the pattern form can be specified *and built* now**, which changes the
scheduling: `REAUDIT46` findings 2–3 can be dropped rather than deferred, because
there will be no injected names to deduplicate diagnostics for.

### And the restriction pays for itself

Admitting only a name reference **removes** readings rather than adding them:

```
  old x + 1     pattern reading needs «x + 1» as the argument -> not a name
                -> refused, so «(old x) + 1» is the only reading
```

An unrestricted hole would have made that ambiguous. So the constraint is not a
tax on the design; it is the thing that keeps `old` out of the ambiguity budget
everywhere except the one case we already measured.

## 6. Summary

| | |
|---|---|
| get it out of the flat name table | **right** |
| a scope with juxtaposed qualification | **does not help** — measured, same unrepairable defect |
| a scope with a symbol separator | sound, but `old.is valid` reads worse |
| `old (_)` as a pattern | **works** — both readings selectable |
| cost | no name may begin `old` — 0.157%, the largest single-word reservation measured |
| `previous (_)` | 0.033%, if that matters more than brevity |
| buys | no injection, no exemption, no shadow duplication, and possibly no REAUDIT46 |
| the argument constraint | **not a blocker** — "is a bare name" and "is reactive" are both structural and already available. Buildable now |
| `old x + 1` | unambiguous *because* of the restriction — it removes readings rather than adding them |
