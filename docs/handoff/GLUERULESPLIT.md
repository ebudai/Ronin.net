# Shape 1 — but for a different reason, and R5′ has a clause missing

> **Ledger** — `[V]` Shape 1 — but for a different reason, and R5′ has a clause missing
> supersedes: none
> superseded by: none

**Yes, R5′ narrows the pattern-glue rule too.** `IS-AND-EQUALITY.md` §4's own
example is pattern glue and I should have said so rather than using the name he
had given me for a rule I had not seen.

His shape 1 is right and his reasoning for it is not quite the whole story —
measuring it turned up a gap in R5′ that neither of us stated, and once it is
plugged, `GlueAsName` stops being a separate legibility rule and becomes the
one-word case of a clause R5′ needs regardless.

---

## 1. He is right that `GlueAsName` is not a capture rule — measured

Patterns `send (_) to (_)`, `send (_)`, `print (_)`; declare the one-word name
`to`:

```
  34 statements parse before declaring «to»
  statements that change when «to» is declared: 0
```

A one-word name cannot straddle the literal it would have to cover, so no
statement changes meaning. His read is confirmed, and it is exactly why the two
findings needed separating rather than sharing `Offender`.

## 2. But R5′ as I sent it has a hole, and it is not a one-word hole

*"No multi-word name may contain a glue word interiorly"* says nothing about a
name made **entirely** of glue words. `to to` has `to` at index 0 and index 1 —
both edges — so R5′ admits it.

```
  send to to to      + «to»                 OK             3  send «to» to «to»
  send to to to to   + «to»                 NO PARSE
  send to to to      + «to» and «to to»     OK             3  send «to» to «to»
  send to to to to   + «to» and «to to»     TIE -> ERROR   3
```

A correction on the way there: **I predicted the four-token form would tie and
it does not.** Placing the literal at the last position leaves the second hole
empty, so there is only one reading. It takes five tokens for both placements to
be viable:

```
  send «to» to «to to»      literal at position 2
  send «to to» to «to»      literal at position 3
```

Both at cost 3, and the statement becomes unwritable. Neither name contains glue
interiorly, so R5′ as written admits both.

So the rule needs a second clause:

> **R5′** — no multi-word name may contain a glue word **interiorly**, and no
> name may consist **wholly** of glue words.

## 3. Which answers his question differently than either of us framed it

The second clause covers `to` and `to to` in one line. So:

**`GlueAsName` is not a legibility rule that happens to survive. It is the
one-word case of a capture rule R5′ was missing.**

That matters for more than tidiness — it changes what the diagnostic should say.
"«to» is a glue word and would be hard to read" is a style complaint. "«to» can
occupy a hole beside the literal it is glue for, so `send to to to to` would
have two readings" is a reason, and it is the same reason `to to` is refused.
One rule, one message, two arities.

It also means shape 2 was never really on the table: narrowing both would leave
the two-word all-glue name admitted, and the statement unwritable with no rule
pointing at why.

## 4. And R5′ dissolves the route `GlueAsName` originally came from

Worth knowing because it removes a reason he might otherwise keep:

`SCOPING.md` had `var seconds` inject `old seconds`, which is multi-word, so
blanket R5 rejected it on glue `seconds`. `GLUE-AS-WHOLE-NAMES.md` §1 noted this
had silently reserved every glue word against every single-word **reactive**
name.

Under R5′, `old seconds` is edge-glue and **admitted**. The shadow route stops
firing. So the capture backing that `GlueAsName` used to inherit is gone — which
is what made his question the right one to ask, and why the answer had to come
from the all-glue clause instead.

**It also closes something for free.** `GLUE-AS-WHOLE-NAMES.md` §2 flagged that
if any pattern ever used `old` as glue, *every* reactive declaration in scope
would produce a diagnostic about an injected name the author cannot rename — I
called it the worst diagnostic outcome in the language. Under R5′, `old
anything` is edge-glue and admitted, so that hole closes without anyone aiming
at it. Worth recording in the supersession table, because the fix now exists and
the finding does not.

## 5. My part of the mix-up

He is right that I was using his name for a rule I had not seen. I wrote *"narrow
`Infixes(names)`"* as though there were one rule, having been told about one
rule, and `IS-AND-EQUALITY.md` §4's `to uppercase` example is about the other
one — pattern glue from `send (_) to (_)`, not an operator word.

The general form, since this is the second time a design document has used an
implementation's vocabulary and inherited its shape: **a design rule should be
stated over the language's own concepts — "glue word", "operator word",
"name" — and the mapping to code names should be his, in one place.** When I
write `Infixes(names)` I am asserting something about a structure I have not
seen.

## 6. Summary

| | |
|---|---|
| does R5′ narrow pattern glue too | **yes** — §4's `to uppercase` is pattern glue, and it was the measured point of the narrowing |
| is `GlueAsName` a capture rule | **no** — measured, 0 of 34 statements change |
| does it survive | **yes**, as **shape 1** — but as the one-word case of a missing clause, not as a legibility rule |
| R5′ as sent | **incomplete** — add *"and no name may consist wholly of glue words"* |
| my four-token prediction | **wrong** — it takes five tokens; corrected in the probe |
| shadow-injection route | stops firing under R5′, which is why the question arose — and it closes `GLUE-AS-WHOLE-NAMES.md` §2's `old`-as-glue hole for free |

Probe: `glue_as_name.py`.
